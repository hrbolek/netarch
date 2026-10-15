using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.Services.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.UI.FormNode.Components;
using WebAppArchitecture.DigitalProcesses.UI.SubmissionNode.Components;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormSectionLayoutTests
{
    [Theory]
    [InlineData("""{"field":{"renderer_key":"input.text"}}""")]
    [InlineData("""{"root":{"renderer_key":"section","max_occurs":3,"configuration":{"visualization":"table"}}}""")]
    [InlineData("""{"root":{"renderer_key":"section","children":{"rows":{"renderer_key":"section","configuration":{"visualization":"table"}}}}}""")]
    [InlineData("""{"root":{"renderer_key":"section","children":{"rows":{"renderer_key":"section","max_occurs":3,"configuration":{"visualization":"table"},"children":{"nested":{"renderer_key":"section"}}}}}}""")]
    [InlineData("""{"root":{"renderer_key":"section","configuration":{"visualization":"unknown"}}}""")]
    public async Task SchemaRejectsInvalidRootAndTableLayouts(string children)
    {
        using var host = new IsolatedHost();
        using var scope = ActorScope(host);
        var forms = scope.ServiceProvider.GetRequiredService<IFormDefinitionService>();
        await Assert.ThrowsAsync<ArgumentException>(() => forms.CreateTypedAsync(
            new("Invalid layout", children: System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(children))));
    }

    [Fact]
    public async Task EveryTreeMutationEnforcesSectionAndTableRules()
    {
        using var host = new IsolatedHost();
        using var scope = ActorScope(host);
        var nodes = scope.ServiceProvider.GetRequiredService<IFormNodeService>();
        var versionId = await SeedVersion(host);
        var root = await nodes.CreateContainedInVersionAsync(new(versionId, "root", DigitalProcessWellKnown.FormNodeTypes.Section));
        var field = await nodes.CreateContainedInNodeAsync(new(versionId, "text", DigitalProcessWellKnown.FormNodeTypes.Text, root.id));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.CreateContainedInVersionAsync(
            new(versionId, "bad", DigitalProcessWellKnown.FormNodeTypes.Text)));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.MoveAsync(field.id, new(null, 0)));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.ImportSubtreeAsync(
            new(field.id, versionId, key_prefix: "copy")));
        var table = await nodes.CreateContainedInNodeAsync(new(versionId, "rows",
            DigitalProcessWellKnown.FormNodeTypes.Section, root.id, max_occurs: 5,
            configuration_json: """{"visualization":"table"}"""));
        await nodes.CreateContainedInNodeAsync(new(versionId, "value", DigitalProcessWellKnown.FormNodeTypes.Text, table.id));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.CreateContainedInNodeAsync(
            new(versionId, "nested", DigitalProcessWellKnown.FormNodeTypes.Section, table.id)));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.MoveAsync(table.id, new(null, 0)));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.UpdateDefinitionAsync(table.id,
            new("rows", configuration_json: """{"visualization":"table"}""", max_occurs: 1)));
        var nested = await nodes.CreateContainedInNodeAsync(new(versionId, "nested",
            DigitalProcessWellKnown.FormNodeTypes.Section, root.id));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.MoveAsync(nested.id, new(table.id, 0)));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.ImportSubtreeAsync(
            new(nested.id, versionId, table.id)));
        await Assert.ThrowsAsync<ArgumentException>(() => nodes.UpdateDefinitionAsync(root.id,
            new(root.key, configuration_json: """{"visualization":"table"}""", max_occurs: 5)));
        using var client = host.CreateClient();
        using var invalidCreate = ActingRequest.Get("/digital-processes/FormNodeResponse/create-in-version",
            RbacWellKnown.BootstrapAdminUserId);
        invalidCreate.Method = HttpMethod.Post;
        invalidCreate.Content = JsonContent.Create(new CreateFormNodeRequest(versionId, "bad", DigitalProcessWellKnown.FormNodeTypes.Text));
        using var createResponse = await client.SendAsync(invalidCreate);
        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
        using var invalidMove = ActingRequest.Get($"/digital-processes/FormNodeResponse/move/{field.id}",
            RbacWellKnown.BootstrapAdminUserId);
        invalidMove.Method = HttpMethod.Put;
        invalidMove.Content = JsonContent.Create(new MoveFormNodeRequest(null, 0));
        using var moveResponse = await client.SendAsync(invalidMove);
        Assert.Equal(HttpStatusCode.BadRequest, moveResponse.StatusCode);
    }

    [Fact]
    public async Task BulkValidatesMergedTreeIncludingOmittedChildSections()
    {
        using var host = new IsolatedHost();
        using var scope = ActorScope(host);
        var versionId = await SeedVersion(host);
        var nodes = scope.ServiceProvider.GetRequiredService<IFormNodeService>();
        var root = await nodes.CreateContainedInVersionAsync(new(versionId, "root", DigitalProcessWellKnown.FormNodeTypes.Section));
        var parent = await nodes.CreateContainedInNodeAsync(new(versionId, "rows",
            DigitalProcessWellKnown.FormNodeTypes.Section, root.id, max_occurs: 5));
        await nodes.CreateContainedInNodeAsync(new(versionId, "nested",
            DigitalProcessWellKnown.FormNodeTypes.Section, parent.id));
        using var client = host.CreateClient();
        foreach (var body in new[]
        {
            JsonNode.Parse("""{"children":{"bad":{"renderer_key":"input.text"}}}""")!,
            JsonNode.Parse($$"""
                {"nodes":[{"id":"{{root.id}}","key":"{{root.key}}","node_type_id":"{{root.node_type_id}}",
                "children":[{"id":"{{parent.id}}","key":"rows","node_type_id":"{{parent.node_type_id}}",
                "max_occurs":5,"configuration_json":"{\"visualization\":\"table\"}"}]}]}
                """)!
        })
        {
            using var request = ActingRequest.Get($"/digital-processes/FormVersionResponse/tree/{versionId}", RbacWellKnown.BootstrapAdminUserId);
            request.Method = HttpMethod.Put;
            request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        Assert.Equal(3, (await nodes.GetTreeAsync(versionId)).nodes.Count);
    }

    [Fact]
    public async Task RuntimeTableRendersOneRowPerOccurrenceAndSelectedLabels()
    {
        using var host = new IsolatedHost();
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var types = db.Set<FormNodeTypeModel>().Select(FormNodeTypeResponse.Projection).ToArray();
        var section = Definition("Rows", DigitalProcessWellKnown.FormNodeTypes.Section, Guid.NewGuid(), """{"visualization":"table"}""");
        var search = Definition("User", DigitalProcessWellKnown.FormNodeTypes.GraphqlSearch, section.id);
        var text = Definition("Note", DigitalProcessWellKnown.FormNodeTypes.Text, section.id);
        var hidden = Definition("Unreadable field", DigitalProcessWellKnown.FormNodeTypes.Text, section.id);
        var first = Value(section.id, null, 0, null);
        var second = Value(section.id, null, 1, null);
        var html = await Render<SubmissionFormNode>(scope.ServiceProvider, new()
        {
            ["Node"] = section, ["Nodes"] = new[] { section, search, text, hidden }, ["Types"] = types,
            ["Values"] = new[] { first, second, Value(search.id, first.id, 0, """{"id":"secret-id-1","label":"First user"}"""),
                Value(search.id, second.id, 0, """{"id":"secret-id-2","label":"Second user"}"""),
                Value(text.id, first.id, 0, "\"First note\""), Value(text.id, second.id, 0, "\"Second note\"") }
        });
        Assert.Equal(2, html.Split("data-repeat-instance").Length - 1);
        Assert.Equal(4, html.Split("data-cell-editor hidden").Length - 1);
        Assert.Contains(">First user</button>", html);
        Assert.Contains(">Second user</button>", html);
        Assert.DoesNotContain(">secret-id-", html);
        Assert.Contains("First note", html);
        Assert.DoesNotContain("Unreadable field", html);
    }

    [Fact]
    public async Task TabsRenderOnlyFirstPanelVisibleAndPreviewRendersTable()
    {
        using var host = new IsolatedHost();
        using var scope = host.Services.CreateScope();
        var first = Definition("First", DigitalProcessWellKnown.FormNodeTypes.Section, null);
        var second = Definition("Second", DigitalProcessWellKnown.FormNodeTypes.Section, null);
        var html = await Render<FormSectionTabs>(scope.ServiceProvider, new()
        {
            ["Sections"] = new[] { first, second },
            ["ChildContent"] = (RenderFragment<FormNodeResponse>)(node => builder => builder.AddContent(0, node.key))
        });
        Assert.Equal(2, html.Split("role=\"tabpanel\"").Length - 1);
        Assert.Contains("aria-selected=\"true\"", html);
        Assert.Contains($"data-section-panel=\"{second.id}\" hidden", html);
        Assert.DoesNotContain($"data-section-panel=\"{first.id}\" hidden", html);
        var section = Definition("Rows", DigitalProcessWellKnown.FormNodeTypes.Section, first.id, """{"visualization":"table"}""") with { min_occurs = 2 };
        var field = Definition("Text", DigitalProcessWellKnown.FormNodeTypes.Text, section.id);
        var types = scope.ServiceProvider.GetRequiredService<CoreDbContext>().Set<FormNodeTypeModel>().Select(FormNodeTypeResponse.Projection).ToArray();
        var preview = await Render<FormNodeTreeItem>(scope.ServiceProvider, new()
        {
            ["Node"] = section, ["Nodes"] = new[] { section, field }, ["Types"] = types, ["TestMode"] = true
        });
        Assert.Contains("<table", preview);
        Assert.Equal(2, preview.Split("data-repeat-instance").Length - 1);
        Assert.Contains("data-repeat-add", preview);
        using var client = host.CreateClient();
        using var script = await client.GetAsync("/_content/DigitalProcesses.UI/digital-processes-sections.js");
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
        Assert.Contains("javascript", script.Content.Headers.ContentType!.MediaType);
    }

    private static async Task<Guid> SeedVersion(IsolatedHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var definition = Guid.NewGuid();
        var version = Guid.NewGuid();
        var actor = RbacWellKnown.BootstrapAdminUserId;
        var rbac = RbacWellKnown.RootSecurityObjectId;
        var now = DateTimeOffset.UtcNow;
        db.Set<FormDefinitionModel>().Add(new(definition, "Layout test", null, rbac, now, now, actor, actor));
        db.Set<FormVersionModel>().Add(new(version, definition, 1, false, null, rbac, now, now, actor, actor));
        await db.SaveChangesAsync();
        return version;
    }

    private static IServiceScope ActorScope(IsolatedHost host)
    {
        var scope = host.Services.CreateScope();
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.Request.Headers["x-ms-client-principal-id"] = RbacWellKnown.BootstrapAdminUserId.ToString();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        return scope;
    }

    private static async Task<string> Render<T>(IServiceProvider services, Dictionary<string, object?> parameters) where T : IComponent
    {
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    }

    private static FormNodeResponse Definition(string key, Guid type, Guid? parent, string? configuration = null)
        => new(Guid.NewGuid(), Guid.NewGuid(), parent, "/", key, key, null, type, 0, 1, 5, false, configuration,
            Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid());

    private static SubmissionNodeResponse Value(Guid definition, Guid? parent, int occurrence, string? json)
        => new(Guid.NewGuid(), Guid.NewGuid(), definition, parent, "/", occurrence, 0, json,
            Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid());
}
