using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormTreeSchemaHttpTests
{
    [Theory]
    [InlineData("ZSC", 65)]
    [InlineData("Material", 68)]
    public async Task FormFile_CanBeImportedDirectlyAndRepeatedWithoutDuplicateNodes(string name, int expected)
    {
        using var host = new IsolatedHost();
        var version = await Seed(host);
        using var client = host.CreateClient();
        var source = JsonNode.Parse(await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, $"Form.{name}.json")))!;
        using var first = await Put(client, version, source);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var initial = (await first.Content.ReadFromJsonAsync<FormTreeResponse>())!;
        Assert.Equal(expected, initial.nodes.Count);
        var root = source["children"]!.AsObject().First();
        root.Value!["label"] = "Updated section";
        using var repeat = await Put(client, version, source);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        var updated = (await repeat.Content.ReadFromJsonAsync<FormTreeResponse>())!;
        Assert.Equal(initial.nodes.Select(x => x.id).Order(), updated.nodes.Select(x => x.id).Order());
        Assert.Equal("Updated section", updated.nodes.Single(x => x.parent_id is null && x.key == root.Key).label);
        Assert.All(updated.nodes, node =>
        {
            using var configuration = JsonDocument.Parse(node.configuration_json!);
            Assert.Equal(JsonValueKind.Object, configuration.RootElement.ValueKind);
        });
        Assert.Contains(updated.nodes, x => x.configuration_json!.Contains("choices:"));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"nodes":null}""")]
    [InlineData("""{"children":[]}""")]
    [InlineData("""{"nodes":[],"children":{}}""")]
    [InlineData("""{"children":{"field":{"renderer_key":"not-a-renderer"}}}""")]
    [InlineData("""{"children":{"field":{"renderer_key":"input.text","configuration":"invalid"}}}""")]
    [InlineData("""{"nodes":[null]}""")]
    public async Task InvalidBody_Returns400InsteadOfUnhandledException(string body)
    {
        using var host = new IsolatedHost();
        var version = await Seed(host);
        using var client = host.CreateClient();
        using var response = await Put(client, version, JsonNode.Parse(body)!);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.False(string.IsNullOrWhiteSpace(problem!["detail"]!.GetValue<string>()));
    }

    [Fact]
    public async Task SchemaAndBulkFormats_PreserveUnspecifiedNodes_AndRejectTypeChangesAndPublishedVersions()
    {
        using var host = new IsolatedHost();
        var version = await Seed(host);
        using var client = host.CreateClient();
        using var bulk = await Put(client, version, new BulkFormTreeRequest(
            [new(null, "existing", DigitalProcessWellKnown.FormNodeTypes.Section, label: "Keep")]));
        Assert.Equal(HttpStatusCode.OK, bulk.StatusCode);
        var old = Assert.Single((await bulk.Content.ReadFromJsonAsync<FormTreeResponse>())!.nodes);
        var schema = JsonNode.Parse("""{"children":{"added":{"renderer_key":"section","label":"New"}}}""")!;
        using var add = await Put(client, version, schema);
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        var nodes = (await add.Content.ReadFromJsonAsync<FormTreeResponse>())!.nodes;
        Assert.Equal(2, nodes.Count);
        Assert.Contains(nodes, x => x.id == old.id && x.label == "Keep");
        using var incompatible = await Put(client, version,
            new BulkFormTreeRequest([new(old.id, "existing", DigitalProcessWellKnown.FormNodeTypes.Text)]));
        Assert.Equal(HttpStatusCode.Conflict, incompatible.StatusCode);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var model = await db.Set<FormVersionModel>().FindAsync(version);
            db.Entry(model!).CurrentValues.SetValues(model! with { is_published = true });
            await db.SaveChangesAsync();
        }
        using var published = await Put(client, version, schema);
        Assert.Equal(HttpStatusCode.Conflict, published.StatusCode);
        using var denied = await Put(client, version, schema, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }

    private static async Task<Guid> Seed(IsolatedHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var definition = Guid.NewGuid(); var version = Guid.NewGuid();
        var actor = RbacWellKnown.BootstrapAdminUserId;
        var rbac = RbacWellKnown.RootSecurityObjectId;
        var now = DateTimeOffset.UtcNow;
        db.Set<FormDefinitionModel>().Add(new(definition, "Import test", null, rbac, now, now, actor, actor));
        db.Set<FormVersionModel>().Add(new(version, definition, 1, false, null, rbac, now, now, actor, actor));
        await db.SaveChangesAsync();
        return version;
    }

    private static async Task<HttpResponseMessage> Put(HttpClient client, Guid id, object body, Guid? actor = null)
    {
        using var request = ActingRequest.Get(
            "/digital-processes/FormVersionResponse/tree/" + id, actor ?? RbacWellKnown.BootstrapAdminUserId);
        request.Method = HttpMethod.Put;
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
}
