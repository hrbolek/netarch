using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class DefinitionResolverLayeringTests
{
    [Fact]
    public async Task DefinitionPagesAndNextVersions_PreserveContractsThroughSecuredServices()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient(new() { AllowAutoRedirect = false });
        var now = DateTimeOffset.UtcNow;
        var actor = RbacWellKnown.BootstrapAdminUserId;
        var form = Guid.NewGuid();
        var workflow = Guid.NewGuid();
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var rbac = RbacWellKnown.RootSecurityObjectId;
            db.Set<FormDefinitionModel>().Add(new(form, "Form version test", null, rbac, now, now, actor, actor));
            db.Set<WorkflowDefinitionModel>().Add(new(workflow, "Workflow version test", null, rbac, now, now, actor, actor));
            db.Set<FormVersionModel>().Add(new(Guid.NewGuid(), form, 4, false, null, rbac, now, now, actor, actor));
            db.Set<WorkflowVersionModel>().Add(new(Guid.NewGuid(), workflow, 7, false, null, rbac, now, now, actor, actor));
            await db.SaveChangesAsync();
        }
        foreach (var (kind, id) in new[] { ("Form", form), ("Workflow", workflow) })
        {
            using var request = ActingRequest.Get($"/digital-processes/{kind}DefinitionResponse/new-version/{id}", actor);
            request.Method = HttpMethod.Post;
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith($"/digital-processes/{kind}VersionResponse/designer/", response.Headers.Location!.OriginalString);
        }
        using (var verify = host.Services.CreateScope())
        {
            var db = verify.ServiceProvider.GetRequiredService<CoreDbContext>();
            Assert.Equal(new[] { 4, 5 }, await db.Set<FormVersionModel>()
                .Where(x => x.form_definition_id == form).OrderBy(x => x.version).Select(x => x.version).ToArrayAsync());
            Assert.Equal(new[] { 7, 8 }, await db.Set<WorkflowVersionModel>()
                .Where(x => x.workflow_definition_id == workflow).OrderBy(x => x.version).Select(x => x.version).ToArrayAsync());
        }
        using var workflowRead = ActingRequest.Get($"/digital-processes/WorkflowDefinitionResponse/view/{workflow}", actor);
        using var detail = await client.SendAsync(workflowRead);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var json = await detail.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
        Assert.Equal(workflow.ToString(), json!["id"]!.ToString());
        Assert.Equal(2, json["versions"]!.AsArray().Count);
    }

    [Fact]
    public async Task ActionRoute_DoesNotExposeUnreadableEntities()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var request = ActingRequest.Get(
            $"/digital-processes/FormVersionResponse/actions/{Guid.NewGuid()}", Guid.NewGuid());
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
