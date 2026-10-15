using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.ProcessDefinitionVersion;
/// <summary>
/// Služba Process Definition Version Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class ProcessDefinitionVersionService(ISecurityLayer security, IDBLayer unsecuredDb) : SecuredBaseService<ProcessDefinitionVersionModel>(security, unsecuredDb),IProcessDefinitionVersionService
{
    public async Task<ApplicantMatrixPageData?> ApplicantMatrixAsync(Guid id, CancellationToken ct = default)
    {
        var process = await Security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Read, ct);
        if (process is null) return null;
        var mayEdit = !process.is_published &&
            await Security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Update, ct) is not null;
        var states = Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Where(x => x.workflow_version_id == process.workflow_version_id)
            .OrderBy(x => x.position)
            .ToArray().Select(WorkflowStateResponse.FromModel).ToArray();
        var nodes = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == process.form_version_id)
            .ToArray().Select(FormNodeResponse.FromModel).ToArray();
        return new(ProcessDefinitionVersionResponse.FromModel(process), mayEdit, states, nodes);
    }

    public Task<ProcessDefinitionVersionResponse> CreateNextAsync(CreateProcessDefinitionVersionRequest request, CancellationToken ct = default)
    {
        var next = Security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read)
            .Where(v => v.process_definition_id == request.process_definition_id)
            .Select(v => v.version).ToArray().DefaultIfEmpty(0).Max() + 1;
        return CreateContainedAsync(request with { version = next }, ct);
    }

    private static void ValidateOwnedDefinitions(ProcessDefinitionModel process, FormVersionModel form, WorkflowVersionModel workflow)
    {
        if (process.form_definition_id is null || process.workflow_definition_id is null)
            throw new InvalidOperationException("Legacy process has no owned form/workflow definitions. Migrate it before creating or publishing versions.");
        if (form.form_definition_id != process.form_definition_id || workflow.workflow_definition_id != process.workflow_definition_id)
            throw new InvalidOperationException("Process versions may use only versions of the process-owned form and workflow definitions.");
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: GetByIdAsync, CurrentUserId; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<ProcessDefinitionVersionResponse> CreateContainedAsync(CreateProcessDefinitionVersionRequest request, CancellationToken ct = default)
    {
        var (process, _) = await CreationScopeGuard.RequireEntityAsync<ProcessDefinitionModel>(
            Security, request.process_definition_id, "contained", ct);
        var form = await Security.GetByIdAsync<FormVersionModel>(request.form_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Form version not found.");
        var workflow = await Security.GetByIdAsync<WorkflowVersionModel>(request.workflow_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Workflow version not found.");
        ValidateOwnedDefinitions(process, form, workflow);

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new ProcessDefinitionVersionModel(
            Guid.NewGuid(), process.id, request.form_version_id, request.workflow_version_id,
            request.version, false, null, process.rbacobject_id,
            now, now, actor, actor) { target_group_type_id = request.target_group_type_id };
        var created = await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return ProcessDefinitionVersionResponse.FromModel(x);
        }, ct);
        return request.publish ? await PublishAsync(created.id, ct) : created;
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: GetByIdAsync, Query, ExecuteInTransactionAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<ProcessDefinitionVersionResponse> CloneLatestAsync(Guid definitionId, CancellationToken ct = default)
    {
        _ = await Security.GetByIdAsync<ProcessDefinitionModel>(definitionId, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Process definition not found or not editable.");
        var previous = Security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read)
            .Where(v => v.process_definition_id == definitionId)
            .OrderByDescending(v => v.version).FirstOrDefault()
            ?? throw new InvalidOperationException("Process has no existing version to copy.");
        var created = await CreateContainedAsync(new CreateProcessDefinitionVersionRequest(
            definitionId, previous.form_version_id, previous.workflow_version_id, previous.version + 1, target_group_type_id: previous.target_group_type_id), ct);
        var rules = Security.Query<FormNodeApplicantAccessModel>(SecurityOperations.Read)
            .Where(x => x.process_definition_version_id == previous.id).ToArray();
        var now = DateTimeOffset.UtcNow;
        await Security.ExecuteInTransactionAsync(async token =>
        {
            foreach (var rule in rules)
                await Security.StageCreateAsync(rule with
                {
                    id = Guid.NewGuid(), process_definition_version_id = created.id,
                    rbacobject_id = created.rbacobject_id,
                    created_at = now, updated_at = now,
                    created_by = Security.CurrentUserId, updated_by = Security.CurrentUserId
                }, token);
            await Security.SaveChangesAsync(token);
        }, ct);
        return created;
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: GetByIdAsync, Query, ExecuteInTransactionAsync, StageDeleteAsync, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task DeleteDraftAsync(Guid id, CancellationToken ct = default)
    {
        var version = await Security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Delete, ct)
            ?? throw new KeyNotFoundException("Process version not found or not deletable.");
        if (version.is_published) throw new InvalidOperationException("Published versions cannot be deleted.");
        if (Security.Query<RequestModel>(SecurityOperations.Read).Any(r => r.process_definition_version_id == id))
            throw new InvalidOperationException("Process version is referenced by a request.");
        await Security.ExecuteInTransactionAsync(async token => {
            var policies = Security.Query<FormNodeApplicantAccessModel>(SecurityOperations.Delete)
                .Where(x => x.process_definition_version_id == id).Select(x => x.id).ToArray();
            foreach (var policyId in policies)
                await Security.StageDeleteAsync<FormNodeApplicantAccessModel>(policyId, token);
            await Security.StageDeleteAsync<ProcessDefinitionVersionModel>(id, token);
            await Security.SaveChangesAsync(token);
        }, ct);
    }

    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: GetByIdAsync, Query, CurrentUserId, ExecuteInTransactionAsync, StageUpdateAsync, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<ProcessDefinitionVersionResponse> PublishAsync(Guid id,CancellationToken ct=default){var model=await Security.GetByIdAsync<ProcessDefinitionVersionModel>(id,SecurityOperations.Update,ct)??throw new KeyNotFoundException();if(model.is_published)return ProcessDefinitionVersionResponse.FromModel(model);var form=await Security.GetByIdAsync<FormVersionModel>(model.form_version_id,SecurityOperations.Read,ct)??throw new KeyNotFoundException();var workflow=await Security.GetByIdAsync<WorkflowVersionModel>(model.workflow_version_id,SecurityOperations.Read,ct)??throw new KeyNotFoundException();var process = await Security.GetByIdAsync<ProcessDefinitionModel>(model.process_definition_id, SecurityOperations.Read, ct) ?? throw new KeyNotFoundException("Process not found.");
        ValidateOwnedDefinitions(process, form, workflow);
        if(!form.is_published||!workflow.is_published)throw new InvalidOperationException("Process version requires published form and workflow versions.");
        if(model.target_group_type_id is not Guid targetType || targetType == Guid.Empty) throw new InvalidOperationException("A TargetGroupTypeId is required before publishing the process version.");
        var stateIds=Security.Query<WorkflowStateModel>(SecurityOperations.Read).Where(x=>x.workflow_version_id==workflow.id).Select(x=>x.id).ToArray();
        var targetNodeIds=Security.Query<WorkflowStateAccessRuleModel>(SecurityOperations.Read).Where(x=>stateIds.Contains(x.workflow_state_id)&&x.target_scope==WorkflowAccessTargetScopes.FormNode&&x.target_form_node_id!=null).Select(x=>x.target_form_node_id!.Value).Distinct().ToArray();
        if(targetNodeIds.Length>0 && Security.Query<FormNodeModel>(SecurityOperations.Read).Any(x=>targetNodeIds.Contains(x.id)&&x.form_version_id!=form.id))
            throw new InvalidOperationException("Workflow state access rules reference FormNodes outside the process FormVersion.");
        var now=DateTimeOffset.UtcNow;var updated=model with{is_published=true,published_at=now,updated_at=now,updated_by=Security.CurrentUserId};return await Security.ExecuteInTransactionAsync(async t=>{var x=await Security.StageUpdateAsync(updated,t);await Security.SaveChangesAsync(t);return ProcessDefinitionVersionResponse.FromModel(x);},ct);}
}
