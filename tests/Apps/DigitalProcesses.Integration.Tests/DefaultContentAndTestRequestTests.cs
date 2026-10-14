using WebAppArchitecture.SecurityLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class DefaultContentAndTestRequestTests
{
    [Fact]
    public async Task NewProcess_HasDraftContent_AndTestRequestWithHistory_AndPublishedOnlyStart()
    {
        using var host = new IsolatedHost();
        using var scope = host.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        ctx.Request.Headers["x-ms-client-principal-id"] = RbacWellKnown.BootstrapAdminUserId.ToString("D");
        accessor.HttpContext = ctx;

        var processes = scope.ServiceProvider.GetRequiredService<IProcessDefinitionService>();
        var requests = scope.ServiceProvider.GetRequiredService<IRequestService>();
        var submissionNodes = scope.ServiceProvider.GetRequiredService<ISubmissionNodeService>();
        var created = await processes.CreateTypedAsync(new CreateProcessDefinitionRequest("Demo"));
        Guid workflowVersionId, processVersionId;
        using (var createdDbScope = host.Services.CreateScope())
        {
            var createdDb = createdDbScope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var initialVersion = await createdDb.Set<ProcessDefinitionVersionModel>()
                .Where(x => x.process_definition_id == created.id)
                .SingleAsync();
            processVersionId = initialVersion.id;
            workflowVersionId = initialVersion.workflow_version_id;
        }

        await GrantStartProcessAsync(host, scope.ServiceProvider, created.id);
        var test = await requests.CreateTestRequestForWorkflowVersionAsync(workflowVersionId);
        var versionTest = await requests.CreateTestRequestForProcessVersionAsync(processVersionId);

        using var verify = host.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<CoreDbContext>();
        var version = await db.Set<ProcessDefinitionVersionModel>().AsNoTracking().SingleAsync(x => x.process_definition_id == created.id);
        Assert.False(version.is_published);
        Assert.Equal(version.id, (await db.Set<RequestModel>().AsNoTracking().SingleAsync(x => x.id == test.id)).process_definition_version_id);
        Assert.Equal(2, await db.Set<FormNodeModel>().CountAsync(x => x.form_version_id == version.form_version_id));
        Assert.Equal(2, await db.Set<WorkflowStateModel>().CountAsync(x => x.workflow_version_id == version.workflow_version_id));
        Assert.Equal(2, await db.Set<WorkflowTransitionModel>().CountAsync(x => x.workflow_version_id == version.workflow_version_id));
        Assert.Equal(2, await db.Set<RequestTransitionHistoryModel>().CountAsync(x => x.request_id == test.id));
        Assert.Equal(2, await db.Set<SubmissionNodeModel>().CountAsync(x =>
            x.submission_id == test.current_submission_id));
        Assert.Equal(processVersionId, (await db.Set<RequestModel>().AsNoTracking().SingleAsync(x => x.id == versionTest.id)).process_definition_version_id);
        Assert.Equal(2, await db.Set<RequestTransitionHistoryModel>().CountAsync(x => x.request_id == versionTest.id));
        Assert.Equal(2, await db.Set<SubmissionNodeModel>().CountAsync(x =>
            x.submission_id == versionTest.current_submission_id));
        var textDefinitionId = await db.Set<FormNodeModel>()
            .Where(x => x.form_version_id == version.form_version_id
                && x.node_type_id == DigitalProcessWellKnown.FormNodeTypes.Text)
            .Select(x => x.id)
            .SingleAsync();
        var inputNodeId = await db.Set<SubmissionNodeModel>()
            .Where(x => x.submission_id == versionTest.current_submission_id
                && x.form_node_id == textDefinitionId)
            .Select(x => x.id)
            .SingleAsync();
        var inputNode = await db.Set<SubmissionNodeModel>().AsNoTracking().SingleAsync(x => x.id == inputNodeId);
        var security = verify.ServiceProvider.GetRequiredService<WebAppArchitecture.SecurityLayer.ISecurityLayer>();
        Assert.True(await security.IsAllowedAsync(inputNode.rbacobject_id, SecurityOperations.Update));
        var updatedNode = await submissionNodes.SetValueAsync(
            inputNodeId, new SetSubmissionNodeValueRequest("\"test value\""));
        Assert.Equal("\"test value\"", updatedNode.value_json);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            requests.StartAsync(new StartRequestRequest(version.id)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            requests.StartAsync(new StartRequestRequest(Guid.Empty, process_definition_id: created.id)));
    }

    private static async Task GrantStartProcessAsync(IsolatedHost host, IServiceProvider services, Guid rbacId)
    {
        using (var grant = host.Services.CreateScope())
        {
            var db = grant.ServiceProvider.GetRequiredService<CoreDbContext>();
            var op = await db.Operations.SingleAsync(x => x.name == "start_process");
            var typesWithOp = db.RoleTypeOperations.Where(x => x.operation_id == op.id).Select(x => x.role_type_id).ToHashSet();
            foreach (var rt in await db.RoleTypes.Select(x => x.id).ToArrayAsync())
                if (!typesWithOp.Contains(rt))
                    db.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), rt, op.id));
            await db.SaveChangesAsync();
        }
        await services.GetRequiredService<ISecurityProjectionService>().SynchronizeRbacObjectAsync(rbacId);
    }
}
