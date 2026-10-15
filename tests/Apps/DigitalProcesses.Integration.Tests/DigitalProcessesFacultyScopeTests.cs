using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DigitalProcesses;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

/// <summary>
/// DP16: An existing faculty administrative-case RBAC branch hosts a process and its
/// request/submission scopes. All HTTP reads exercise the real authorization filter.
/// The fixture seeds a persisted runtime snapshot to isolate RBAC from workflow commands;
/// command-driven lifecycle remains a separate acceptance scenario.
/// </summary>
public sealed class DigitalProcessesFacultyScopeTests
{
    [Fact]
    public async Task DP16_FacultyCaseRole_CanReadSubmission_UnaffiliatedUserCannot()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var f = await SeedAsync(host);
        using var granted = await client.SendAsync(ActingRequest.Get(
            "/digital-processes/SubmissionResponse/list", f.Officer));
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        var visible = await granted.Content.ReadFromJsonAsync<List<SubmissionRecord>>();
        Assert.NotNull(visible);
        Assert.Contains(visible, x => x.id == f.Submission);

        using var denied = await client.SendAsync(ActingRequest.Get(
            "/digital-processes/SubmissionResponse/list", f.Unrelated));
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        var hidden = await denied.Content.ReadFromJsonAsync<List<SubmissionRecord>>();
        Assert.NotNull(hidden);
        Assert.DoesNotContain(hidden, x => x.id == f.Submission);
    }

    [Fact]
    public async Task DP17_FacultyCaseScope_DoesNotGrantOtherFacultySubmission()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var f = await SeedAsync(host);
        using var response = await client.SendAsync(ActingRequest.Get(
            "/digital-processes/SubmissionResponse/list", f.Officer));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var visible = await response.Content.ReadFromJsonAsync<List<SubmissionRecord>>();
        Assert.NotNull(visible);
        Assert.DoesNotContain(visible, x => x.id == f.OtherSubmission);
    }

    [Fact]
    public async Task DP18_ArchivedSnapshot_RemainsPersistedAndSeparateFromActiveSubmission()
    {
        using var host = new IsolatedHost();
        var f = await SeedAsync(host);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var request = await db.Set<RequestModel>().AsNoTracking().SingleAsync(x => x.id == f.Request);
        var snapshots = await db.Set<SubmissionModel>().AsNoTracking().Where(x => x.request_id == f.Request)
            .OrderBy(x => x.version).ToListAsync();
        Assert.Equal(2, snapshots.Count);
        Assert.True(snapshots[0].is_archived);
        Assert.NotNull(snapshots[0].archived_at);
        Assert.False(snapshots[1].is_archived);
        Assert.NotEqual(snapshots[0].rbacobject_id, snapshots[1].rbacobject_id);
        Assert.Equal(snapshots[1].id, request.current_submission_id);
    }

    private sealed record SubmissionRecord(Guid id);
    private sealed record FixtureIds(Guid Officer, Guid Unrelated, Guid Request, Guid Submission, Guid OtherSubmission);

    private static async Task<FixtureIds> SeedAsync(IsolatedHost host)
    {
        // Force startup/bootstrap before resolving a scope.
        using var client = host.CreateClient();
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var projection = scope.ServiceProvider.GetRequiredService<ISecurityProjectionService>();
        var now = DateTimeOffset.UtcNow;
        var admin = RbacWellKnown.BootstrapAdminUserId;
        var officer = Guid.NewGuid();
        var unrelated = Guid.NewGuid();
        var roleType = Guid.NewGuid();
        var faculty = Guid.NewGuid();
        var caseGroup = Guid.NewGuid();
        var process = Guid.NewGuid();
        var request = Guid.NewGuid();
        var oldSubmission = Guid.NewGuid();
        var submission = Guid.NewGuid();
        var otherFaculty = Guid.NewGuid();
        var otherCase = Guid.NewGuid();
        var otherSubmission = Guid.NewGuid();
        var foreignRequestId = Guid.NewGuid();
        var root = await db.Groups.SingleAsync(x => x.id == RbacWellKnown.RootGroupId);
        GroupModel Group(Guid id, string name, GroupModel parent, bool inherit = true) =>
            new(id, name, parent.group_type_id, parent.id, parent.path + $"{id:D}/",
                inherit, id, now, now, admin, admin);
        var facultyGroup = Group(faculty, "Faculty A", root);
        var caseScope = Group(caseGroup, "Administrative proceedings", facultyGroup);
        var processGroup = Group(process, "Student application process", caseScope);
        var requestGroup = Group(request, "Request", processGroup, false);
        var archivedGroup = Group(oldSubmission, "Archived submission", requestGroup, false);
        var activeGroup = Group(submission, "Active submission", requestGroup, false);
        var foreignFaculty = Group(otherFaculty, "Faculty B", root);
        var foreignCase = Group(otherCase, "Other faculty case", foreignFaculty);
        var foreignRequestGroup = Group(foreignRequestId, "Other request", foreignCase, false);
        var foreignSubmission = Group(otherSubmission, "Other faculty submission", foreignRequestGroup, false);
        db.Groups.AddRange(facultyGroup, caseScope, processGroup, requestGroup,
            archivedGroup, activeGroup, foreignFaculty, foreignCase, foreignRequestGroup, foreignSubmission);
        db.Users.AddRange(
            new UserModel(officer, "Faculty officer", null, null, root.id, now, now, admin, admin),
            new UserModel(unrelated, "Unrelated user", null, null, root.id, now, now, admin, admin));
        db.RoleTypes.Add(new RoleTypeModel(roleType, "Faculty case officer " + roleType.ToString("N"), root.id,
            now, now, admin, admin));
        var readableOperations = await db.Operations
            .Where(x => x.name == SecurityOperations.Read || x.name == SecurityOperations.List)
            .ToListAsync();
        Assert.Equal(2, readableOperations.Count);
        foreach (var operation in readableOperations)
            db.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), roleType, operation.id));
        db.Roles.Add(new RoleModel(Guid.NewGuid(), caseScope.id, officer, roleType,
            false, now.AddDays(-1), null, null, caseScope.id, now, now, admin, admin));
        // Request and Submission are explicit inheritance boundaries. State-scoped
        // bindings map the faculty's functional role to each runtime security scope.
        foreach (var target in new[] { request, oldSubmission, submission })
            db.RbacAccessBindings.Add(new RbacAccessBindingModel(Guid.NewGuid(),
                caseScope.id, roleType, target, roleType, now.AddDays(-1), null,
                null, caseScope.id, now, now, admin, admin));

        var formId = Guid.NewGuid(); var formVersionId = Guid.NewGuid();
        var workflowId = Guid.NewGuid(); var workflowVersionId = Guid.NewGuid();
        var stateId = Guid.NewGuid(); var processVersionId = Guid.NewGuid();
        db.Set<FormDefinitionModel>().Add(new FormDefinitionModel(formId, "Student form", null, process, now, now, admin, admin));
        db.Set<FormVersionModel>().Add(new FormVersionModel(formVersionId, formId, 1, true, now, process, now, now, admin, admin));
        db.Set<WorkflowDefinitionModel>().Add(new WorkflowDefinitionModel(workflowId, "Student workflow", null, process, now, now, admin, admin));
        db.Set<WorkflowVersionModel>().Add(new WorkflowVersionModel(workflowVersionId, workflowId, 1, true, now, process, now, now, admin, admin));
        db.Set<WorkflowStateModel>().Add(new WorkflowStateModel(stateId, workflowVersionId, "submitted", "Submitted", null,
            true, false, 0, process, now, now, admin, admin));
        db.Set<ProcessDefinitionModel>().Add(new ProcessDefinitionModel(process, "Administrative process", null,
            roleType, process, now, now, admin, admin));
        db.Set<ProcessDefinitionVersionModel>().Add(new ProcessDefinitionVersionModel(processVersionId,
            process, formVersionId, workflowVersionId, 1, true, now, process, now, now, admin, admin));
        db.Set<RequestModel>().Add(new RequestModel(request, processVersionId, officer,
            stateId, submission, "Application", request, now, now, admin, admin));
        db.Set<SubmissionModel>().AddRange(
            new SubmissionModel(oldSubmission, request, formVersionId, 1, true,
                now, oldSubmission, now, now, admin, admin),
            new SubmissionModel(submission, request, formVersionId, 2, false,
                null, submission, now, now, admin, admin),
            new SubmissionModel(otherSubmission, foreignRequestId, formVersionId, 1, false,
                null, otherSubmission, now, now, admin, admin));
        db.Set<RequestModel>().Add(new RequestModel(foreignRequestId, processVersionId,
            unrelated, stateId, otherSubmission, "Other request", foreignRequestId,
            now, now, admin, admin));
        await db.SaveChangesAsync();
        await projection.SynchronizeGroupSubtreeAsync(caseScope.id);
        return new(officer, unrelated, request, submission, otherSubmission);
    }
}
