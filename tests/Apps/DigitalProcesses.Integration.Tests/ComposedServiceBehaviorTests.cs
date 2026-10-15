using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.TemplateFieldBinding;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Services.Shared;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using WebAppArchitecture.Tests.Shared;
using WebAppArchitecture.SecurityLayer;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class ComposedServiceBehaviorTests
{
    [Fact]
    public async Task TemplatesPreserveOwnerAuthorizationAtomicRevisionsAndCascadeDeletion()
    {
        using var host = new IsolatedHost();
        using var scope = ActorScope(host);
        var seed = await Seed(scope.ServiceProvider);
        var templates = scope.ServiceProvider.GetRequiredService<IRtfTemplateService>();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var bytes = Encoding.ASCII.GetBytes(@"{\rtf1 Hello [[I:text]]}");
        using var content = new MemoryStream(bytes);
        var uploaded = await templates.UploadAsync(seed.form_version_id, "application/rtf", content, "Composition");
        Assert.True(uploaded.Found);
        Assert.Null(uploaded.Error);
        var template = Assert.IsType<RtfTemplateInfo>(uploaded.Template);
        Assert.Equal(bytes, (await templates.DownloadAsync(template.Id))!.Content);
        Assert.Single((await templates.ListAsync(seed.form_version_id))!);
        Assert.Single((await templates.ListRevisionsAsync(template.Id))!);
        var versions = scope.ServiceProvider.GetRequiredService<IFormVersionService>();
        var page = await versions.TemplatePageAsync(seed.form_version_id);
        Assert.NotNull(page);
        var listedTemplate = Assert.Single(page.Model.templates!);
        Assert.Equal(template.Id, listedTemplate.Id);
        Assert.Equal(template.FormVersionId, listedTemplate.FormVersionId);
        Assert.Equal(template.Name, listedTemplate.Name);
        Assert.True(page.MayEdit);
        var ordinaryVersion = FormVersionResponse.FromModel(
            (await db.Set<FormVersionModel>().FindAsync(seed.form_version_id))!);
        Assert.Null(ordinaryVersion.templates);
        Assert.DoesNotContain("\"templates\"", JsonSerializer.Serialize(ordinaryVersion));

        using (var client = host.CreateClient(new() { AllowAutoRedirect = false }))
        {
            foreach (var representation in new[] { "html", "fragment" })
            {
                using var request = ActingRequest.Get("/architecture/root-permissions", RbacWellKnown.BootstrapAdminUserId);
                request.Headers.Add("X-Response-Type", representation);
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("Create Form Definitions", await response.Content.ReadAsStringAsync());
            }
            using var templatesRequest = ActingRequest.Get(
                $"/digital-processes/FormVersionResponse/templates/{seed.form_version_id}", RbacWellKnown.BootstrapAdminUserId);
            using var templatesResponse = await client.SendAsync(templatesRequest);
            Assert.Equal(HttpStatusCode.OK, templatesResponse.StatusCode);
            var html = await templatesResponse.Content.ReadAsStringAsync();
            Assert.Contains("Composition", html);
            Assert.Contains($"/digital-processes/RtfTemplate/{template.Id}", html);

            using var legacy = await client.GetAsync($"/digital-processes/form-versions/{seed.form_version_id}/templates");
            Assert.Equal(HttpStatusCode.Redirect, legacy.StatusCode);
            Assert.Equal($"/digital-processes/FormVersionResponse/templates/{seed.form_version_id}",
                legacy.Headers.Location!.OriginalString);
        }

        var rejected = await templates.CreateRevisionAsync(template.Id,
            new(Convert.ToBase64String(bytes), [new("missing", Guid.NewGuid())]));
        Assert.NotNull(rejected.Error);
        Assert.Single((await templates.ListRevisionsAsync(template.Id))!);
        Assert.False(await db.Set<TemplateFieldBindingModel>().AnyAsync());

        var revisedBytes = Encoding.ASCII.GetBytes(@"{\rtf1 Updated [[I:text]]}");
        var revision = await templates.CreateRevisionAsync(template.Id,
            new(Convert.ToBase64String(revisedBytes), [new("text", seed.node_id)]));
        Assert.Null(revision.Error);
        Assert.Equal(2, revision.Revision!.revision);
        Assert.Equal(revisedBytes, (await templates.DownloadRevisionAsync(revision.Revision.id))!.Content);
        Assert.Equal(revisedBytes, (await templates.DownloadAsync(template.Id))!.Content);
        Assert.Equal(seed.node_id, (await db.Set<TemplateFieldBindingModel>().SingleAsync()).form_node_id);
        Assert.NotNull(await templates.GetFormSchemaAsync(seed.form_version_id));

        using (var denied = ActorScope(host, Guid.NewGuid()))
        {
            var inaccessible = denied.ServiceProvider.GetRequiredService<IRtfTemplateService>();
            Assert.Null(await inaccessible.ListAsync(seed.form_version_id));
            Assert.Null(await denied.ServiceProvider.GetRequiredService<IFormVersionService>()
                .TemplatePageAsync(seed.form_version_id));
            Assert.Null(await inaccessible.DownloadAsync(template.Id));
            Assert.Null(await inaccessible.DownloadRevisionAsync(revision.Revision.id));
            Assert.Null(await inaccessible.GetFormSchemaAsync(seed.form_version_id));
            Assert.Null(await inaccessible.ListRevisionsAsync(template.Id));
            using var deniedContent = new MemoryStream(bytes);
            Assert.False((await inaccessible.UploadAsync(seed.form_version_id, "application/rtf", deniedContent, "Denied")).Found);
            Assert.False((await inaccessible.GenerateAsync(seed.form_version_id)).Found);
            Assert.False((await inaccessible.CreateRevisionAsync(template.Id, new(Convert.ToBase64String(bytes), null))).Found);
            Assert.False(await inaccessible.DeleteTemplateAsync(template.Id));
            Assert.Equal(404, (await denied.ServiceProvider.GetRequiredService<IFormDocumentService>()
                .SchemaAsync(seed.form_version_id, default)).StatusCode);
        }
        SetActor(scope.ServiceProvider, RbacWellKnown.BootstrapAdminUserId);
        Assert.Equal(2, (await templates.ListRevisionsAsync(template.Id))!.Count);
        Assert.True(await templates.DeleteTemplateAsync(template.Id));
        db.ChangeTracker.Clear();
        Assert.False(await db.Set<DocumentTemplateModel>().AnyAsync(item => item.id == template.Id));
        Assert.False(await db.Set<DocumentTemplateRevisionModel>().AnyAsync(item => item.template_id == template.Id));
        Assert.False(await db.Set<TemplateFieldBindingModel>().AnyAsync(item => item.form_node_id == seed.node_id));
    }

    [Fact]
    public async Task ApplicantPolicyDelegatesWritesToItsEntityAndPreservesDraftAndActorChecks()
    {
        using var host = new IsolatedHost();
        using var scope = ActorScope(host);
        var seed = await Seed(scope.ServiceProvider);
        var policies = scope.ServiceProvider.GetRequiredService<IApplicantPolicyService>();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var body = new SetApplicantVisibilityRequest(seed.state_id, seed.node_id, true);
        Assert.Equal(204, (await policies.SetAsync(seed.id, body, default)).StatusCode);
        var rules = db.Set<FormNodeApplicantAccessModel>().AsNoTracking().Where(rule =>
            rule.process_definition_version_id == seed.id && rule.workflow_state_id == seed.state_id && rule.form_node_id == seed.node_id);
        var created = await rules.SingleAsync();
        Assert.Equal(seed.rbacobject_id, created.rbacobject_id);
        Assert.Equal(RbacWellKnown.BootstrapAdminUserId, created.created_by);
        Assert.Equal(204, (await policies.SetAsync(seed.id, body with { CanRead = false }, default)).StatusCode);
        Assert.False((await rules.SingleAsync()).can_read);
        Assert.Equal(200, policies.List(seed.id, seed.state_id).StatusCode);

        using (var denied = ActorScope(host, Guid.NewGuid()))
        {
            var inaccessible = denied.ServiceProvider.GetRequiredService<IApplicantPolicyService>();
            Assert.Equal(404, inaccessible.List(seed.id, null).StatusCode);
            Assert.Equal(404, (await inaccessible.SetAsync(seed.id, body, default)).StatusCode);
            var actions = denied.ServiceProvider.GetRequiredService<IDigitalProcessActionService<ProcessDefinitionVersionModel>>();
            Assert.False(await actions.CanReadAsync(seed.id));
        }
        SetActor(scope.ServiceProvider, RbacWellKnown.BootstrapAdminUserId);
        Assert.True(await scope.ServiceProvider.GetRequiredService<IDigitalProcessActionService<ProcessDefinitionVersionModel>>()
            .CanReadAsync(seed.id));
        db.ChangeTracker.Clear();
        var version = await db.Set<ProcessDefinitionVersionModel>().SingleAsync(x => x.id == seed.id);
        db.Entry(version).CurrentValues.SetValues(version with { is_published = true, published_at = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        Assert.Equal(409, (await policies.SetAsync(seed.id, body, default)).StatusCode);
        Assert.False((await rules.SingleAsync()).can_read);
    }

    [Fact]
    public async Task AiApprovalUsesSecuredVersionWritesAndRejectsInaccessibleOrPublishedVersions()
    {
        using var host = new IsolatedHost();
        using var scope = ActorScope(host);
        var seed = await Seed(scope.ServiceProvider);
        var ai = scope.ServiceProvider.GetRequiredService<IFormDesignAiService>();
        var documents = scope.ServiceProvider.GetRequiredService<IFormDocumentService>();
        Assert.Equal(200, (await documents.SchemaAsync(seed.form_version_id, default)).StatusCode);
        Assert.Equal(200, scope.ServiceProvider.GetRequiredService<IFormComponentCatalogService>().GetCatalog().StatusCode);
        using var agent = new LocalAiAgent();
        var proposalService = new FormDesignAiService(
            scope.ServiceProvider.GetRequiredService<IFormVersionService>(),
            scope.ServiceProvider.GetRequiredService<IFormNodeService>(),
            scope.ServiceProvider.GetRequiredService<IFormNodeTypeService>(),
            agent,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiAgents:formdesign_aiagent:enabled"] = "true",
                ["AiAgents:formdesign_aiagent:endpoint"] = "https://local-ai.test/design"
            }).Build(),
            NullLoggerFactory.Instance);
        Assert.Equal(200, (await proposalService.DesignAsync(new(seed.form_version_id, "Edit the form"), default)).StatusCode);
        Assert.Equal(seed.form_version_id.ToString(), agent.Payload!.RootElement
            .GetProperty("context").GetProperty("form_version_id").GetString());
        Assert.Equal("input.text", agent.Payload.RootElement.GetProperty("context").GetProperty("form_schema")
            .GetProperty("children").GetProperty("section").GetProperty("children").GetProperty("text")
            .GetProperty("renderer_key").GetString());
        var schema = JsonSerializer.SerializeToElement(new
        {
            children = new
            {
                section = new
                {
                    renderer_key = "section",
                    children = new { text = new { renderer_key = "input.text", label = "Approved by composition" } }
                }
            }
        });
        var approval = new ApproveFormDesignRequest(seed.form_version_id, schema);
        Assert.Equal(200, (await ai.ApproveAsync(approval, default)).StatusCode);
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        Assert.Equal(2, await db.Set<FormNodeModel>().CountAsync(node => node.form_version_id == seed.form_version_id));
        Assert.Equal("Approved by composition", (await db.Set<FormNodeModel>().AsNoTracking()
            .SingleAsync(node => node.id == seed.node_id)).label);

        using (var denied = ActorScope(host, Guid.NewGuid()))
        {
            var inaccessible = denied.ServiceProvider.GetRequiredService<IFormDesignAiService>();
            Assert.Equal(404, (await inaccessible.ApproveAsync(approval, default)).StatusCode);
            Assert.Equal(404, (await inaccessible.DesignAsync(new(seed.form_version_id, "Edit the form"), default)).StatusCode);
        }
        SetActor(scope.ServiceProvider, RbacWellKnown.BootstrapAdminUserId);
        db.ChangeTracker.Clear();
        var version = await db.Set<FormVersionModel>().SingleAsync(x => x.id == seed.form_version_id);
        db.Entry(version).CurrentValues.SetValues(version with { is_published = true, published_at = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        Assert.Equal(400, (await ai.ApproveAsync(approval, default)).StatusCode);
        Assert.Equal(2, await db.Set<FormNodeModel>().CountAsync(node => node.form_version_id == seed.form_version_id));
    }

    private sealed record Seeded(Guid id, Guid form_version_id, Guid node_id, Guid state_id, Guid rbacobject_id);

    [Fact]
    public async Task ReadOnlyPermissionsStillPermitCatalogReadsButNeverAiApproval()
    {
        using var host = new IsolatedHost();
        using var scope = ActorScope(host);
        var seed = await Seed(scope.ServiceProvider);
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var actor = RbacWellKnown.BootstrapAdminUserId;
        var scopeId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var root = await db.Groups.SingleAsync(group => group.id == RbacWellKnown.RootGroupId);
        db.Groups.Add(new(scopeId, "Read-only composition scope", root.group_type_id, root.id,
            root.path + $"{scopeId:D}/", false, scopeId, now, now, actor, actor));
        var roleType = Guid.NewGuid();
        db.RoleTypes.Add(new(roleType, $"Composition read only {roleType:N}", root.id, now, now, actor, actor));
        var read = await db.Operations.SingleAsync(operation => operation.name == SecurityOperations.Read);
        db.RoleTypeOperations.Add(new(Guid.NewGuid(), roleType, read.id));
        db.Roles.Add(new(Guid.NewGuid(), scopeId, actor, roleType, false, now.AddMinutes(-1), null, null, scopeId, now, now, actor, actor));
        var version = await db.Set<FormVersionModel>().SingleAsync(item => item.id == seed.form_version_id);
        db.Entry(version).CurrentValues.SetValues(version with { rbacobject_id = scopeId });
        var node = await db.Set<FormNodeModel>().SingleAsync(item => item.id == seed.node_id);
        var type = await db.Set<FormNodeTypeModel>().SingleAsync(item => item.id == node.node_type_id);
        db.Entry(type).CurrentValues.SetValues(type with { rbacobject_id = scopeId });
        await db.SaveChangesAsync();
        await scope.ServiceProvider.GetRequiredService<ISecurityProjectionService>().SynchronizeRbacObjectAsync(scopeId);
        var security = scope.ServiceProvider.GetRequiredService<ISecurityLayer>();
        Assert.True(await security.IsAllowedAsync(scopeId, SecurityOperations.Read));
        Assert.False(await security.IsAllowedAsync(scopeId, SecurityOperations.List));
        Assert.False(await security.IsAllowedAsync(scopeId, SecurityOperations.Update));

        var types = scope.ServiceProvider.GetRequiredService<IFormNodeTypeService>();
        Assert.DoesNotContain(types.Query<FormNodeTypeResponse>().ToArray(), item => item.id == type.id);
        var catalog = scope.ServiceProvider.GetRequiredService<IFormComponentCatalogService>().GetCatalog();
        Assert.Equal(200, catalog.StatusCode);
        Assert.Contains(JsonSerializer.SerializeToElement(catalog.Value).GetProperty("components").EnumerateArray(),
            component => component.GetProperty("id").GetGuid() == type.id);
        var ai = scope.ServiceProvider.GetRequiredService<IFormDesignAiService>();
        Assert.Equal(404, (await ai.ApproveAsync(new(seed.form_version_id,
            JsonSerializer.SerializeToElement(new { children = new { } })), default)).StatusCode);
        Assert.Equal(2, await db.Set<FormNodeModel>().CountAsync(item => item.form_version_id == seed.form_version_id));
    }

    private sealed class LocalAiAgent : HttpMessageHandler, IHttpClientFactory
    {
        public JsonDocument? Payload { get; private set; }

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { result = new { } }) };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Payload?.Dispose();
            base.Dispose(disposing);
        }
    }

    private static async Task<Seeded> Seed(IServiceProvider services)
    {
        var process = await services.GetRequiredService<IProcessDefinitionService>()
            .CreateTypedAsync(new CreateProcessDefinitionRequest("Composition test"));
        var db = services.GetRequiredService<CoreDbContext>();
        var version = await db.Set<ProcessDefinitionVersionModel>().SingleAsync(x => x.process_definition_id == process.id);
        var node = await db.Set<FormNodeModel>().FirstAsync(x => x.form_version_id == version.form_version_id && x.parent_id != null);
        var state = await db.Set<WorkflowStateModel>().FirstAsync(x => x.workflow_version_id == version.workflow_version_id);
        return new(version.id, version.form_version_id, node.id, state.id, version.rbacobject_id);
    }

    private static IServiceScope ActorScope(IsolatedHost host, Guid? actor = null)
    {
        var scope = host.Services.CreateScope();
        SetActor(scope.ServiceProvider, actor ?? RbacWellKnown.BootstrapAdminUserId);
        return scope;
    }

    private static void SetActor(IServiceProvider services, Guid actor)
    {
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Headers["x-ms-client-principal-id"] = actor.ToString();
        services.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
    }
}
