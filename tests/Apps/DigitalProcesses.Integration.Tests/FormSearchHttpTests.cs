using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormSearchHttpTests
{
    [Fact]
    public async Task SearchScript_IsServedAsJavaScript()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/_content/DigitalProcesses.UI/digital-processes-search.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType ?? "");
        Assert.Contains("data-graphql-search", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UserList_AppliesCaseInsensitiveJsonFilterBeforePaging()
    {
        using var host = new IsolatedHost();
        var userIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var matchingIds = userIds.Take(2).ToArray();
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var now = DateTimeOffset.UtcNow;
            var actor = RbacWellKnown.BootstrapAdminUserId;
            foreach (var (id, email) in new (Guid, string?)[]
            {
                (matchingIds[0], "MiChAl@example.test"),
                (matchingIds[1], "other.ichal@example.test"),
                (userIds[2], "unrelated@example.test"),
                (userIds[3], null)
            })
                db.Set<UserModel>().Add(new UserModel(id, "Filter test", email, null,
                    RbacWellKnown.RootSecurityObjectId, now, now, actor, actor));
            await db.SaveChangesAsync();
        }
        using var client = host.CreateClient();
        var where = Uri.EscapeDataString(JsonSerializer.Serialize(new
        {
            email = new { _ilike = "%ichal%" },
            id = new { _in = userIds }
        }));
        using var response = await client.SendAsync(ActingRequest.Get(
            "/architecture/UserResponse/list?where=" + where, RbacWellKnown.BootstrapAdminUserId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<JsonArray>();
        Assert.NotNull(rows);
        Assert.Equal(matchingIds.Order(), rows.Select(row => row!["id"]!.GetValue<Guid>()).Order());
    }

    [Fact]
    public async Task Search_UsesFixedResponseNames_AndPreservesFormAndConstantVariables()
    {
        using var isolated = new IsolatedHost();
        using var handler = new SearchBackendHandler();
        using var host = isolated.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IFormDataBackend>(sp => new GraphqlLikeFormDataBackend(
                new HttpClient(handler, disposeHandler: false),
                new FormDataBackendOptions { Endpoint = "https://search-backend.test/graphql" },
                NullLogger<GraphqlLikeFormDataBackend>.Instance,
                sp.GetRequiredService<IHttpContextAccessor>()))));

        Guid searchId, userId, versionId;
        using (var scope = host.Services.CreateScope())
        {
            var children = JsonSerializer.Deserialize<JsonElement>("""
                {
                  "user": { "renderer_key": "input.graphql-search",
                    "configuration": { "query": "query { choices { id label } }" } },
                  "search": {
                    "renderer_key": "input.graphql-search",
                    "configuration": {
                      "query": "query search($fragment: Str, $surname: Str, $selectedId: Str) { choices: userList(where: { email: { _ilike: $fragment }, surname: { _eq: $surname }, id: { _eq: $selectedId } }) { id label: email } }",
                      "variables": { "fragment": "must be overridden", "surname": "Example", "selectedId": "$.form.user.id" },
                      "path": "data.wrong",
                      "valueField": "wrong",
                      "labelField": "wrong"
                    }
                  }
                }
                """);
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var actor = RbacWellKnown.BootstrapAdminUserId;
            var rbac = RbacWellKnown.RootSecurityObjectId;
            var now = DateTimeOffset.UtcNow;
            var definitionId = Guid.NewGuid();
            versionId = Guid.NewGuid();
            userId = Guid.NewGuid();
            searchId = Guid.NewGuid();
            db.Set<FormDefinitionModel>().Add(new FormDefinitionModel(definitionId,
                "Search regression", null, rbac, now, now, actor, actor));
            db.Set<FormVersionModel>().Add(new FormVersionModel(versionId,
                definitionId, 1, false, null, rbac, now, now, actor, actor));
            foreach (var (key, id) in new[] { ("user", userId), ("search", searchId) })
                db.Set<FormNodeModel>().Add(new FormNodeModel(id, versionId, null,
                    $"/{id:D}", key, key, null, DigitalProcessWellKnown.FormNodeTypes.GraphqlSearch,
                    key == "user" ? 0 : 1, 1, 1, false,
                    children.GetProperty(key).GetProperty("configuration").GetRawText(),
                    rbac, now, now, actor, actor));
            await db.SaveChangesAsync();
        }
        using var client = host.CreateClient();
        using (var preview = ActingRequest.Get($"/digital-processes/FormVersionResponse/test/{versionId}",
            RbacWellKnown.BootstrapAdminUserId))
        {
            preview.Headers.Accept.Clear();
            preview.Headers.Accept.ParseAdd("text/html");
            using var response = await client.SendAsync(preview);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("data-graphql-search", html);
            Assert.Contains("/_content/DigitalProcesses.UI/digital-processes-search.js", html);
        }

        using (var shortRequest = ActingRequest.Get("/digital-processes/form-node-search", RbacWellKnown.BootstrapAdminUserId))
        {
            shortRequest.Method = HttpMethod.Post;
            shortRequest.Content = JsonContent.Create(new { nodeId = searchId, pattern = "ab" });
            using var response = await client.SendAsync(shortRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(handler.Request);
        }

        using var request = ActingRequest.Get("/digital-processes/form-node-search", RbacWellKnown.BootstrapAdminUserId);
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new
        {
            nodeId = searchId,
            pattern = " %ichal% ",
            values = new[] { new { name = $"node-{userId:N}", value = new { id = "chosen-user", label = "Selected" } } }
        });
        using var result = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var body = await result.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(42, body!["data"]!["choices"]![0]!["id"]!.GetValue<int>());
        Assert.Equal("Michal", body["data"]!["choices"]![0]!["label"]!.GetValue<string>());
        Assert.NotNull(handler.Request);
        Assert.Equal("%ichal%", handler.Request["variables"]!["fragment"]!.GetValue<string>());
        Assert.Equal("Example", handler.Request["variables"]!["surname"]!.GetValue<string>());
        Assert.Equal("chosen-user", handler.Request["variables"]!["selectedId"]!.GetValue<string>());

        var malformedConfiguration = "{\"query\":\"query userread($fragment: Str) {\r\n choices { id label } }\"}";
        string validConfiguration;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var node = await db.Set<FormNodeModel>().SingleAsync(x => x.id == searchId);
            validConfiguration = node.configuration_json!;
            db.Entry(node).CurrentValues.SetValues(node with { configuration_json = malformedConfiguration });
            await db.SaveChangesAsync();
        }
        using (var invalidSearch = ActingRequest.Get("/digital-processes/form-node-search", RbacWellKnown.BootstrapAdminUserId))
        {
            invalidSearch.Method = HttpMethod.Post;
            invalidSearch.Content = JsonContent.Create(new { nodeId = searchId, pattern = "icha" });
            using var response = await client.SendAsync(invalidSearch);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
            Assert.Contains("configuration_json", problem!["detail"]!.GetValue<string>());
            Assert.Contains("\\n", problem["detail"]!.GetValue<string>());
        }
        using (var invalidSave = ActingRequest.Get(
            "/digital-processes/FormVersionResponse/designer/update-form", RbacWellKnown.BootstrapAdminUserId))
        {
            invalidSave.Method = HttpMethod.Post;
            invalidSave.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = searchId.ToString(),
                ["form_version_id"] = versionId.ToString(),
                ["key"] = "search",
                ["min_occurs"] = "1",
                ["max_occurs"] = "1",
                ["configuration_json"] = malformedConfiguration
            });
            using var response = await client.SendAsync(invalidSave);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("configuration_json", await response.Content.ReadAsStringAsync());
        }
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var node = await db.Set<FormNodeModel>().SingleAsync(x => x.id == searchId);
            var configuration = JsonNode.Parse(validConfiguration)!.AsObject();
            configuration["query"] = configuration["query"]!.GetValue<string>().Replace(" { choices:", "\r\n { choices:");
            db.Entry(node).CurrentValues.SetValues(node with { configuration_json = configuration.ToJsonString() });
            await db.SaveChangesAsync();
        }
        using var multilineSearch = ActingRequest.Get("/digital-processes/form-node-search", RbacWellKnown.BootstrapAdminUserId);
        multilineSearch.Method = HttpMethod.Post;
        multilineSearch.Content = JsonContent.Create(new
        {
            nodeId = searchId,
            pattern = "icha",
            values = new[] { new { name = $"node-{userId:N}", value = new { id = "chosen-user", label = "Selected" } } }
        });
        using var multilineResult = await client.SendAsync(multilineSearch);
        Assert.Equal(HttpStatusCode.OK, multilineResult.StatusCode);
        Assert.Contains("\r\n", handler.Request!["query"]!.GetValue<string>());
    }

    private sealed class SearchBackendHandler : HttpMessageHandler
    {
        public JsonObject? Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!.AsObject();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { data = new { choices = new[] { new { id = 42, label = "Michal" } } } })
            };
        }
    }
}
