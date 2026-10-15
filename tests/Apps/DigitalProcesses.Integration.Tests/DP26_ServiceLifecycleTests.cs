using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.Services.Request;
using WebAppArchitecture.DigitalProcesses.Services.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DigitalProcesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

/// <summary>
/// Published-definition fixture + REAL runtime SERVICE commands:
/// Start -> edit -> transition -> archival -> immutable historical snapshot.
/// Design-time HTTP creation/publication and fine-grained multi-actor state policies
/// are NOT exercised by this fixture and require separate acceptance scenarios.
/// </summary>
public sealed class DigitalProcessesFullLifecycleTests
{
    [Fact]
    public async Task DP26_PublishedProcess_StartEditTransition_ArchivesImmutableSnapshot()
    {
        using var host = new IsolatedHost();
        var f = await SeedPublishedProcessAsync(host);

        // Same DI services and SecurityLayer as HTTP, without serialization/routing.
        // SecurityLayer.CurrentUserId still needs a current actor: supply a scoped
        // HttpContext with the same EasyAuth header as the old HTTP test.
        using var serviceScope = host.Services.CreateScope();
        var accessor = serviceScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var previousContext = accessor.HttpContext;
        var actorContext = new DefaultHttpContext { RequestServices = serviceScope.ServiceProvider };
        actorContext.Request.Headers["x-ms-client-principal-id"] = f.Actor.ToString("D");
        accessor.HttpContext = actorContext;
        try
        {
        var requestService = serviceScope.ServiceProvider.GetRequiredService<IRequestService>();
        var nodeService = serviceScope.ServiceProvider.GetRequiredService<ISubmissionNodeService>();
        var started = await requestService.StartAsync(
            new StartRequestRequest(f.ProcessVersion, "Faculty application", f.TargetGroup));

        Guid originalSubmission;
        Guid originalNode;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var request = await db.Set<RequestModel>().AsNoTracking().SingleAsync(x => x.id == started.id);
            Assert.Equal(f.InitialState, request.current_state_id);
            Assert.Equal(f.ProcessVersion, request.process_definition_version_id);
            originalSubmission = request.current_submission_id;
            var first = await db.Set<SubmissionModel>().AsNoTracking().SingleAsync(x => x.id == originalSubmission);
            Assert.Equal(1, first.version);
            Assert.False(first.is_archived);
            var node = await db.Set<SubmissionNodeModel>().AsNoTracking().SingleAsync(x => x.submission_id == first.id);
            Assert.Equal(f.FormNode, node.form_node_id);
            originalNode = node.id;
        }

        await nodeService.SetValueAsync(originalNode, new SetSubmissionNodeValueRequest("\"Original answer\""));
        await requestService.TransitionAsync(started.id, new TransitionRequestRequest(f.Transition));

        Guid nextNode;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var request = await db.Set<RequestModel>().AsNoTracking().SingleAsync(x => x.id == started.id);
            Assert.Equal(f.FinalState, request.current_state_id);
            Assert.NotEqual(originalSubmission, request.current_submission_id);
            var versions = await db.Set<SubmissionModel>().AsNoTracking().Where(x => x.request_id == started.id)
                .OrderBy(x => x.version).ToListAsync();
            Assert.Equal(2, versions.Count);
            Assert.Equal(originalSubmission, versions[0].id);
            Assert.True(versions[0].is_archived);
            Assert.NotNull(versions[0].archived_at);
            Assert.False(versions[1].is_archived);
            Assert.Equal(2, versions[1].version);
            Assert.NotEqual(versions[0].rbacobject_id, versions[1].rbacobject_id);
            var nodes = await db.Set<SubmissionNodeModel>().AsNoTracking()
                .Where(x => x.submission_id == originalSubmission || x.submission_id == request.current_submission_id)
                .ToListAsync();
            Assert.Equal(2, nodes.Count);
            Assert.All(nodes, x => Assert.Equal("\"Original answer\"", x.value_json));
            Assert.NotEqual(nodes[0].id, nodes[1].id);
            nextNode = nodes.Single(x => x.submission_id == request.current_submission_id).id;
            var history = await db.Set<RequestTransitionHistoryModel>().AsNoTracking().Where(x => x.request_id == started.id).ToListAsync();
            Assert.Single(history);
            Assert.Equal(originalSubmission, history[0].archived_submission_id);
        }

        // Even the actor who created the snapshot cannot rewrite archived data.
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await nodeService.SetValueAsync(originalNode,
                new SetSubmissionNodeValueRequest("\"Tampered\"")));
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            Assert.Equal("\"Original answer\"", (await db.Set<SubmissionNodeModel>().AsNoTracking().SingleAsync(x => x.id == originalNode)).value_json);
            Assert.Equal("\"Original answer\"", (await db.Set<SubmissionNodeModel>().AsNoTracking().SingleAsync(x => x.id == nextNode)).value_json);
        }
        }
        finally
        {
            accessor.HttpContext = previousContext;
        }
    }

    private sealed record Fixture(Guid Actor, Guid TargetGroup, Guid ProcessVersion,
        Guid InitialState, Guid FinalState, Guid Transition, Guid FormNode);

    private static async Task<Fixture> SeedPublishedProcessAsync(IsolatedHost host)
    {
        using var client = host.CreateClient(); // Initializes DB and bootstrap identities.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var now = DateTimeOffset.UtcNow;
        var actor = RbacWellKnown.BootstrapAdminUserId;
        var root = await db.Groups.SingleAsync(x => x.id == RbacWellKnown.RootGroupId);
        var targetId = Guid.NewGuid();
        var target = new GroupModel(targetId, "Faculty administrative office", root.group_type_id,
            root.id, root.path + $"{targetId:D}/", true, targetId, now, now, actor, actor);
        db.Groups.Add(target);
        var roleTypeId = Guid.NewGuid();
        db.RoleTypes.Add(new RoleTypeModel(roleTypeId, "Applicant " + roleTypeId.ToString("N"),
            root.id, now, now, actor, actor));
        var ops = await db.Operations.Where(x =>
            x.name == "read" || x.name == "list" || x.name == "create" || x.name == "update" || x.name == "start_process" || x.name == "Read" || x.name == "List" || x.name == "Create" || x.name == "Update").ToListAsync();
        foreach (var op in ops)
            db.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), roleTypeId, op.id));
        db.Roles.Add(new RoleModel(Guid.NewGuid(), targetId, actor, roleTypeId,
            false, now.AddMinutes(-5), null, null, targetId, now, now, actor, actor));

        var form = Guid.NewGuid(); var formVersion = Guid.NewGuid();
        var node = Guid.NewGuid();
        var wf = Guid.NewGuid(); var wfVersion = Guid.NewGuid();
        var initial = Guid.NewGuid(); var final = Guid.NewGuid(); var transition = Guid.NewGuid();
        var process = Guid.NewGuid(); var processVersion = Guid.NewGuid();
        db.Set<FormDefinitionModel>().Add(new FormDefinitionModel(form, "Faculty form", null, root.id, now, now, actor, actor));
        db.Set<FormVersionModel>().Add(new FormVersionModel(formVersion, form, 1, true, now, root.id, now, now, actor, actor));
        var type = DigitalProcessWellKnown.FormNodeTypes.Text;
        Assert.True(await db.Set<FormNodeTypeModel>().AnyAsync(x => x.id == type),
            "Bootstrap must seed the canonical text FormNodeType.");
        db.Set<FormNodeModel>().Add(new FormNodeModel(node, formVersion, null, $"/{node:D}/", "reason", "Reason", null,
            type, 0, 1, 1, false, null, root.id, now, now, actor, actor));
        db.Set<WorkflowDefinitionModel>().Add(new WorkflowDefinitionModel(wf, "Faculty workflow", null, root.id, now, now, actor, actor));
        db.Set<WorkflowVersionModel>().Add(new WorkflowVersionModel(wfVersion, wf, 1, true, now, root.id, now, now, actor, actor));
        db.Set<WorkflowStateModel>().AddRange(
            new WorkflowStateModel(initial, wfVersion, "draft", "Draft", null, true, false, 0, root.id, now, now, actor, actor),
            new WorkflowStateModel(final, wfVersion, "submitted", "Submitted", null, false, true, 1, root.id, now, now, actor, actor));
        db.Set<WorkflowTransitionModel>().Add(new WorkflowTransitionModel(transition, wfVersion, initial, final,
            "submit", "Submit", false, null, null, root.id, now, now, actor, actor));
        db.Set<ProcessDefinitionModel>().Add(new ProcessDefinitionModel(process, "Application", null, roleTypeId,
            root.id, now, now, actor, actor));
        db.Set<ProcessDefinitionVersionModel>().Add(new ProcessDefinitionVersionModel(processVersion, process,
            formVersion, wfVersion, 1, true, now, root.id, now, now, actor, actor)
            { target_group_type_id = root.group_type_id });
        await db.SaveChangesAsync();
        var projection = scope.ServiceProvider.GetRequiredService<WebAppArchitecture.SecurityLayer.ISecurityProjectionService>();
        await projection.SynchronizeGroupSubtreeAsync(targetId);
        return new(actor, targetId, processVersion, initial, final, transition, node);
    }

}
