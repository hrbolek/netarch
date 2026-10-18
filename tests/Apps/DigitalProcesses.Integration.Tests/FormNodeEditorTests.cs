using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormNodeEditorTests
{
    [Fact]
    public async Task DesignerScriptsAreServedAsStaticJavaScript()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        foreach (var (script, behavior) in new[]
        {
            ("form-ai-designer", "function initAiDesigner()"),
            ("node-actions", "details.dp-node-actions[open]"),
            ("form-preview", "window.digitalProcessesRenumber = function(root)")
        })
        {
            using var response = await client.GetAsync($"/_content/DigitalProcesses.UI/digital-processes-{script}.js");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("javascript", response.Content.Headers.ContentType!.MediaType);
            Assert.Contains(behavior, await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task PersistentEditorSupportsFormSectionAndFieldWithoutReparenting()
    {
        using var host = new IsolatedHost();
        var seed = await Seed(host);
        using var client = host.CreateClient(new() { AllowAutoRedirect = false });
        foreach (var (path, root) in new[]
        {
            ($"/digital-processes/FormVersionResponse/designer/{seed.version}", (Guid?)null),
            ($"/digital-processes/FormNodeResponse/editor/{seed.section}", (Guid?)seed.section),
            ($"/digital-processes/FormNodeResponse/editor/{seed.field}", (Guid?)seed.field)
        })
        {
            using var response = await client.SendAsync(ActingRequest.Get(path, RbacWellKnown.BootstrapAdminUserId));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var model = await response.Content.ReadFromJsonAsync<FormDesignerPageResponse>();
            Assert.Equal(root, model!.root_node_id);
            Assert.Equal(root == seed.field ? 1 : 2, model.designer.nodes.Count);
            if (root == seed.field)
            {
                Assert.Equal(seed.section, Assert.Single(model.designer.nodes).parent_id);
                Assert.Equal(seed.section, Assert.Single(model.ancestor_nodes!).id);
            }
            using var htmlRequest = ActingRequest.Get(path, RbacWellKnown.BootstrapAdminUserId);
            htmlRequest.Headers.Accept.ParseAdd("text/html");
            htmlRequest.Headers.Add("X-Response-Type", "html");
            using var htmlResponse = await client.SendAsync(htmlRequest);
            Assert.Equal(HttpStatusCode.OK, htmlResponse.StatusCode);
            var html = await htmlResponse.Content.ReadAsStringAsync();
            Assert.Contains("data-node-properties", html);
            Assert.Contains("data-node-wizard", html);
            Assert.Contains("data-wizard-library", html);
            Assert.DoesNotContain("<strong>Library</strong>", html);
            Assert.Contains("Open subtree editor", html);
            Assert.Contains($"data-insertion-return-url=\"/digital-processes/FormVersionResponse/designer/{seed.version}\"", html);
            Assert.Contains("digital-processes-node-editor.js?v=", html);
            foreach (var card in new[] { "<strong>Add node</strong>", "data-template-management-form-version" })
            {
                if (root is null) Assert.Contains(card, html);
                else Assert.DoesNotContain(card, html);
            }
            if (root == seed.field) Assert.Contains("$.form.section.field", html);
        }
        using var update = ActingRequest.Get("/digital-processes/FormVersionResponse/designer/update-form", RbacWellKnown.BootstrapAdminUserId);
        update.Method = HttpMethod.Post;
        update.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = seed.field.ToString(), ["editor_root_id"] = seed.section.ToString(),
            ["form_version_id"] = seed.version.ToString(), ["key"] = "field", ["label"] = "Changed",
            ["min_occurs"] = "1", ["max_occurs"] = "1", ["configuration_json"] = "{}"
        });
        using var updated = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.Redirect, updated.StatusCode);
        Assert.StartsWith($"/digital-processes/FormNodeResponse/editor/{seed.section}", updated.Headers.Location!.OriginalString);
        using var scope = host.Services.CreateScope();
        var field = await scope.ServiceProvider.GetRequiredService<CoreDbContext>().Set<FormNodeModel>().FindAsync(seed.field);
        Assert.Equal(seed.section, field!.parent_id);
        Assert.Equal("Changed", field.label);
    }

    [Fact]
    public async Task DraftCopiesOnlyReadableSourceAndDoesNotWriteUntilInsert()
    {
        using var host = new IsolatedHost();
        var seed = await Seed(host);
        using var scope = ActorScope(host);
        var service = scope.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        var draft = await service.PrepareAsync(new(source_node_id: seed.section));
        Assert.Equal(2, draft.nodes.Count);
        Assert.DoesNotContain(draft.nodes, x => x.id == seed.section || x.id == seed.field);
        var root = draft.nodes.Single(x => x.id == draft.root_id);
        var child = draft.nodes.Single(x => x.parent_id == root.id);
        Assert.Contains(child.id.ToString(), root.configuration_json);
        Assert.Contains(child.id.ToString("N"), root.configuration_json);
        Assert.True(JsonNode.Parse(root.configuration_json!)!.AsObject().ContainsKey(child.id.ToString()));
        var edited = draft with { nodes = draft.nodes.Select(x => x with { label = "Draft only" }).ToArray() };
        var workspace = await service.WorkspaceAsync(new(seed.version, edited));
        Assert.All(workspace.preview_nodes, node => Assert.Equal("Draft only", node.label));
        using var verify = host.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<CoreDbContext>();
        Assert.Equal(2, await db.Set<FormNodeModel>().CountAsync(x => x.form_version_id == seed.version));
        Assert.Equal("Source section", (await db.Set<FormNodeModel>().FindAsync(seed.section))!.label);
        var shallow = await service.PrepareAsync(new(source_node_id: seed.section, include_children: false));
        Assert.Single(shallow.nodes);
    }

    [Fact]
    public async Task InsertionRemapsReferencesAndDerivesTargetSecurityAuditAndOrder()
    {
        using var host = new IsolatedHost();
        var source = await Seed(host);
        var target = await Seed(host);
        var targetRbac = Guid.NewGuid();
        using (var setup = host.Services.CreateScope())
        {
            var setupDb = setup.ServiceProvider.GetRequiredService<CoreDbContext>();
            var parent = await setupDb.Groups.SingleAsync(x => x.id == RbacWellKnown.RootGroupId);
            var now = DateTimeOffset.UtcNow;
            var actor = RbacWellKnown.BootstrapAdminUserId;
            setupDb.Groups.Add(new(targetRbac, "Insertion scope", parent.group_type_id, parent.id,
                parent.path + $"{targetRbac:D}/", true, targetRbac, now, now, actor, actor));
            var node = (await setupDb.Set<FormNodeModel>().FindAsync(target.section))!;
            setupDb.Entry(node).CurrentValues.SetValues(node with { rbacobject_id = targetRbac });
            await setupDb.SaveChangesAsync();
            await setup.ServiceProvider.GetRequiredService<ISecurityProjectionService>().SynchronizeRbacObjectAsync(targetRbac);
        }
        using var scope = ActorScope(host);
        var service = scope.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        var draft = await service.PrepareAsync(new(source_node_id: source.section));
        var result = await service.InsertAsync(new(target.version, target.section, 0, draft));
        Assert.Equal(2, result.Count);
        var root = result[0];
        var child = result[1];
        Assert.Equal(target.section, root.parent_id);
        Assert.Equal(root.id, child.parent_id);
        Assert.Contains(root.id.ToString("N"), child.path);
        Assert.Contains(child.id.ToString(), root.configuration_json);
        Assert.Contains(child.id.ToString("N"), root.configuration_json);
        Assert.True(JsonNode.Parse(root.configuration_json!)!.AsObject().ContainsKey(child.id.ToString()));
        Assert.All(result, x =>
        {
            Assert.DoesNotContain(draft.nodes, node => node.id == x.id);
            Assert.Equal(targetRbac, x.rbacobject_id);
            Assert.False(x.own_security_scope);
            Assert.Equal(RbacWellKnown.BootstrapAdminUserId, x.created_by);
            Assert.Equal(target.version, x.form_version_id);
        });
        using var verify = host.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<CoreDbContext>();
        Assert.Equal(1, (await db.Set<FormNodeModel>().FindAsync(target.field))!.position);
        Assert.Equal(2, await db.Set<FormNodeModel>().CountAsync(x => x.form_version_id == source.version));
    }

    [Fact]
    public async Task LibrarySearchIsPagedAndFiltersConfigurationTypeRendererAndSource()
    {
        using var host = new IsolatedHost();
        var seed = await Seed(host);
        using var scope = ActorScope(host);
        var service = scope.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        var page = await service.SearchAsync(new(configuration: "NEEDLE", form_version_id: seed.version, limit: 1));
        Assert.Equal(2, page.total);
        Assert.Single(page.items);
        var next = await service.SearchAsync(new(configuration: "needle", form_version_id: seed.version, offset: 1, limit: 1));
        Assert.NotEqual(page.items[0].node.id, next.items[0].node.id);
        var filtered = await service.SearchAsync(new(search: "source", node_type_id: DigitalProcessWellKnown.FormNodeTypes.Text,
            renderer_key: "input.text", configuration: "needle", form_version_id: seed.version));
        Assert.Equal(seed.field, Assert.Single(filtered.items).node.id);
        var decoded = await service.SearchAsync(new(configuration: "\u017e\u00e1dost", form_version_id: seed.version));
        Assert.Equal(seed.section, Assert.Single(decoded.items).node.id);
        using var denied = ActorScope(host, Guid.NewGuid());
        Assert.Empty((await denied.ServiceProvider.GetRequiredService<IFormNodeInsertionService>().SearchAsync(new(configuration: "needle"))).items);
    }

    [Fact]
    public async Task ReadableTargetWithoutCreateRejectsInsertAndLibraryHidesPrivateNodes()
    {
        using var host = new IsolatedHost();
        var seed = await Seed(host);
        var scopeId = Guid.NewGuid();
        var actor = RbacWellKnown.BootstrapAdminUserId;
        using (var arrange = host.Services.CreateScope())
        {
            var db = arrange.ServiceProvider.GetRequiredService<CoreDbContext>();
            var now = DateTimeOffset.UtcNow;
            var root = await db.Groups.SingleAsync(x => x.id == RbacWellKnown.RootGroupId);
            db.Groups.Add(new(scopeId, "Private insertion target", root.group_type_id, root.id, root.path + $"{scopeId:D}/",
                false, scopeId, now, now, actor, actor));
            var typeId = Guid.NewGuid();
            db.RoleTypes.Add(new(typeId, $"Draft read only {typeId:N}", root.id, now, now, actor, actor));
            var readOperation = await db.Operations.SingleAsync(x => x.name == "read");
            db.RoleTypeOperations.Add(new(Guid.NewGuid(), typeId, readOperation.id));
            db.Roles.Add(new(Guid.NewGuid(), scopeId, actor, typeId, false, now.AddMinutes(-1), null, null, scopeId, now, now, actor, actor));
            var node = (await db.Set<FormNodeModel>().FindAsync(seed.section))!;
            db.Entry(node).CurrentValues.SetValues(node with { rbacobject_id = scopeId });
            await db.SaveChangesAsync();
            await arrange.ServiceProvider.GetRequiredService<ISecurityProjectionService>().SynchronizeRbacObjectAsync(scopeId);
        }
        using var scope = ActorScope(host);
        var security = scope.ServiceProvider.GetRequiredService<IAuthorizationLayer>();
        Assert.True(await security.IsAllowedAsync(scopeId, SecurityOperations.Read));
        Assert.False(await security.IsAllowedAsync(scopeId, SecurityOperations.Create));
        var service = scope.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        var draft = await service.PrepareAsync(new(node_type_id: DigitalProcessWellKnown.FormNodeTypes.Text));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.InsertAsync(new(seed.version, seed.section, 0, draft)));
        using var client = host.CreateClient();
        using var request = ActingRequest.Get("/digital-processes/FormNodeResponse/insert-draft", actor);
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new InsertFormNodeDraftRequest(seed.version, seed.section, 0, draft));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var denied = ActorScope(host, Guid.NewGuid());
        var deniedService = denied.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => deniedService.PrepareAsync(new(source_node_id: seed.section)));
        Assert.Empty((await deniedService.SearchAsync(new(form_version_id: seed.version))).items);
        using var verify = host.Services.CreateScope();
        Assert.Equal(2, await verify.ServiceProvider.GetRequiredService<CoreDbContext>().Set<FormNodeModel>().CountAsync(x => x.form_version_id == seed.version));
    }

    [Fact]
    public async Task RendererExamplesUseExistingMetadataAndWorkspaceRealComponents()
    {
        using var host = new IsolatedHost();
        var seed = await Seed(host);
        using var scope = ActorScope(host);
        var service = scope.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        var examples = service.Examples();
        var text = Assert.Single(examples, x => x.type.renderer_key == "input.text");
        var draft = await service.PrepareAsync(new(node_type_id: text.type.id));
        Assert.Equal(text.configuration_json, Assert.Single(draft.nodes).configuration_json);
        using var client = host.CreateClient();
        foreach (var endpoint in new[] { "workspace", "preview" })
        {
            using var request = ActingRequest.Get("/digital-processes/FormNodeResponse/draft/" + endpoint, RbacWellKnown.BootstrapAdminUserId);
            request.Method = HttpMethod.Post;
            request.Headers.Add("X-Response-Type", "html");
            request.Content = JsonContent.Create(new FormNodeDraftWorkspaceRequest(seed.version, draft));
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("data-draft-preview", html);
            Assert.Contains("name=\"node-", html);
            if (endpoint == "workspace") Assert.Contains("data-node-properties", html);
        }
        using var galleryRequest = ActingRequest.Get("/digital-processes/FormNodeResponse/renderer-examples", RbacWellKnown.BootstrapAdminUserId);
        galleryRequest.Headers.Add("X-Response-Type", "html");
        using var gallery = await client.SendAsync(galleryRequest);
        Assert.Equal(HttpStatusCode.OK, gallery.StatusCode);
        var rendered = await gallery.Content.ReadAsStringAsync();
        Assert.Contains("data-renderer-gallery", rendered);
        Assert.Contains("data-graphql-search", rendered);
        Assert.Contains("type=\"checkbox\"", rendered);
    }

    [Fact]
    public async Task InvalidStructurePublishedTargetAndDuplicateKeysNeverPartiallyInsert()
    {
        using var host = new IsolatedHost();
        var seed = await Seed(host);
        using var scope = ActorScope(host);
        var service = scope.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        var leaf = await service.PrepareAsync(new(node_type_id: DigitalProcessWellKnown.FormNodeTypes.Text));
        await Assert.ThrowsAsync<ArgumentException>(() => service.InsertAsync(new(seed.version, null, 0, leaf)));
        var source = await service.PrepareAsync(new(source_node_id: seed.section));
        await Assert.ThrowsAsync<ArgumentException>(() => service.InsertAsync(new(seed.version, null, 0, source)));
        var cycle = source with { nodes = source.nodes.Select(x => x.id == source.root_id ? x with { parent_id = source.nodes[1].id } : x).ToArray() };
        await Assert.ThrowsAsync<ArgumentException>(() => service.WorkspaceAsync(new(seed.version, cycle)));
        using (var verify = host.Services.CreateScope())
        {
            var db = verify.ServiceProvider.GetRequiredService<CoreDbContext>();
            var version = (await db.Set<FormVersionModel>().FindAsync(seed.version))!;
            db.Entry(version).CurrentValues.SetValues(version with { is_published = true });
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InsertAsync(new(seed.version, seed.section, 0, leaf)));
    }

    [Fact]
    public async Task OpenApiIncludesUniqueOperationIdsAndPagingAndRepresentationContracts()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var swagger = await client.GetFromJsonAsync<JsonObject>("/swagger/v1/swagger.json");
        var paths = swagger!["paths"]!;
        Assert.Equal("FormNodeLibrary", paths["/digital-processes/FormNodeResponse/library"]!["get"]!["operationId"]!.ToString());
        Assert.Equal("FormNodeInsertDraft", paths["/digital-processes/FormNodeResponse/insert-draft"]!["post"]!["operationId"]!.ToString());
        var content = paths["/digital-processes/FormNodeResponse/editor/{id}"]!["get"]!["responses"]!["200"]!["content"]!.AsObject();
        Assert.Contains("text/html", content.Select(x => x.Key));
        Assert.Contains("application/json", content.Select(x => x.Key));
    }

    [Fact]
    public async Task DetachedSearchUsesDraftConfigurationAndPreviewValuesWithoutSavingThem()
    {
        using var original = new IsolatedHost();
        using var handler = new DraftSearchHandler();
        using var host = original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IFormDataBackend>(sp => new GraphqlLikeFormDataBackend(
                new HttpClient(handler, false), new() { Endpoint = "https://draft-search.test/graphql" },
                NullLogger<GraphqlLikeFormDataBackend>.Instance, sp.GetRequiredService<IHttpContextAccessor>()))));
        var seed = await Seed(host);
        using var scope = ActorScope(host);
        var insertion = scope.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        var draft = await insertion.PrepareAsync(new(node_type_id: DigitalProcessWellKnown.FormNodeTypes.GraphqlSearch));
        var root = Assert.Single(draft.nodes);
        draft = draft with { nodes = [root with { configuration_json =
            """{"query":"query test($fragment: Str, $extra: Str) { choices: userList { id label: email } }","variables":{"extra":"$.form.input_graphql_search.id"}}""" }] };
        using var client = host.CreateClient();
        using var request = ActingRequest.Get("/digital-processes/FormNodeResponse/draft/search", RbacWellKnown.BootstrapAdminUserId);
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new FormNodeDraftSearchRequest(new(seed.version, draft), root.id, "Michal",
            [new($"node-{root.id:N}", JsonSerializer.SerializeToElement(new { id = "preview-selected", label = "Preview" }))]));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var choices = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Draft choice", choices!["data"]!["choices"]![0]!["label"]!.ToString());
        Assert.Equal("Michal", handler.Body!["variables"]!["fragment"]!.ToString());
        Assert.Equal("preview-selected", handler.Body["variables"]!["extra"]!.ToString());
        using var verify = host.Services.CreateScope();
        Assert.Equal(2, await verify.ServiceProvider.GetRequiredService<CoreDbContext>().Set<FormNodeModel>()
            .CountAsync(x => x.form_version_id == seed.version));
    }

    [Fact]
    public async Task QualifiedInternalReferencesFollowDraftRenamesAndDestinationWhileExternalReferencesRemain()
    {
        using var host = new IsolatedHost();
        var source = await Seed(host);
        var target = await Seed(host);
        using var scope = ActorScope(host);
        var service = scope.ServiceProvider.GetRequiredService<IFormNodeInsertionService>();
        var draft = await service.PrepareAsync(new(source_node_id: source.section));
        var child = draft.nodes.Single(x => x.parent_id == draft.root_id);
        draft = draft with { nodes = draft.nodes.Select(x => x with
        {
            key = x.id == draft.root_id ? "renamed" : "renamed_field"
        }).ToArray() };
        var workspace = await service.WorkspaceAsync(new(target.version, draft));
        var configuration = JsonNode.Parse(workspace.draft.nodes.Single(x => x.id == draft.root_id).configuration_json!)!;
        Assert.Equal("$.form.renamed.renamed_field.id", configuration["selector"]!.ToString());
        Assert.Equal("$.form.other.field.id", configuration["externalSelector"]!.ToString());
        var inserted = await service.InsertAsync(new(target.version, target.section, 0, workspace.draft));
        configuration = JsonNode.Parse(inserted.Single(x => x.key == "renamed").configuration_json!)!;
        Assert.Equal("$.form.section.renamed.renamed_field.id", configuration["selector"]!.ToString());
        Assert.Equal("$.form.other.field.id", configuration["externalSelector"]!.ToString());
        var shallow = await service.PrepareAsync(new(source_node_id: source.field, include_children: false));
        Assert.Equal("$.form.field", shallow.reference_paths![shallow.root_id]);
        Assert.NotEqual(child.id, shallow.root_id);
    }

    private sealed class DraftSearchHandler : HttpMessageHandler
    {
        public JsonObject? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!.AsObject();
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { data = new { choices = new[] { new { id = "draft-choice", label = "Draft choice" } } } }) };
        }
    }

    private sealed record Seeded(Guid version, Guid section, Guid field);
    private static async Task<Seeded> Seed(WebApplicationFactory<Program> host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var definition = Guid.NewGuid(); var version = Guid.NewGuid(); var section = Guid.NewGuid(); var field = Guid.NewGuid();
        var actor = RbacWellKnown.BootstrapAdminUserId; var rbac = RbacWellKnown.RootSecurityObjectId; var now = DateTimeOffset.UtcNow;
        db.Set<FormDefinitionModel>().Add(new(definition, "Editor source", null, rbac, now, now, actor, actor));
        db.Set<FormVersionModel>().Add(new(version, definition, 1, false, null, rbac, now, now, actor, actor));
        db.Set<FormNodeModel>().Add(new(section, version, null, $"/{section:N}", "section", "Source section", null,
            DigitalProcessWellKnown.FormNodeTypes.Section, 0, 1, 1, true,
            JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["marker"] = "needle", ["linkedNodeId"] = field, ["template"] = "node-" + field.ToString("N"),
                [field.ToString()] = "child reference", ["caption"] = "\u017d\u00e1dost",
                ["selector"] = "$.form.section.field.id", ["externalSelector"] = "$.form.other.field.id"
            }),
            rbac, now, now, actor, actor));
        db.Set<FormNodeModel>().Add(new(field, version, section, $"/{section:N}/{field:N}", "field", "Source field", null,
            DigitalProcessWellKnown.FormNodeTypes.Text, 0, 1, 1, true, """{"text":"needle value"}""", rbac, now, now, actor, actor));
        await db.SaveChangesAsync();
        return new(version, section, field);
    }

    private static IServiceScope ActorScope(WebApplicationFactory<Program> host, Guid? actor = null)
    {
        var scope = host.Services.CreateScope();
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.Request.Headers["x-ms-client-principal-id"] = (actor ?? RbacWellKnown.BootstrapAdminUserId).ToString();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        return scope;
    }
}
