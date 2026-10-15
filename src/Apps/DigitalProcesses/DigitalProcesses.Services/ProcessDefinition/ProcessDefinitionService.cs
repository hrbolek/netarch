using WebAppArchitecture.DigitalProcesses.Services.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.Services.Shared;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.ProcessDefinition;
/// <summary>
/// Služba Process Definition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class ProcessDefinitionService
    : SecuredBaseService<ProcessDefinitionModel>, IProcessDefinitionService
{
    private readonly IDBLayer _db;
    private readonly ISecurityProjectionService _securityProjection;

    public async Task<ProcessDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var definition = await Security.GetByIdAsync<ProcessDefinitionModel>(id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Definition not found.");
        var versions = Security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read)
            .Where(v => v.process_definition_id == id).OrderByDescending(v => v.version)
            .Select(ProcessDefinitionVersionResponse.Projection).ToArray();
        return new(ProcessDefinitionResponse.FromModel(definition), versions);
    }

    public ProcessDefinitionService(ISecurityLayer security, ISecurityProjectionService securityProjection, IDBLayer db)
        : base(security, db)
    {
        _db = db;
        _securityProjection = securityProjection;
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: GetByIdAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<ProcessDefinitionResponse> CreateTypedAsync(CreateProcessDefinitionRequest request, CancellationToken ct = default)
    {
        var initiatorRoleTypeId = request.initiator_role_type_id
            ?? DigitalProcessWellKnown.InitiatorRoleTypeId;

        _ = await Security.GetByIdAsync<RoleTypeModel>(initiatorRoleTypeId, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException(
                $"Initiator role type '{initiatorRoleTypeId}' was not found or is not readable.");
        var (catalog, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, RbacWellKnown.ProcessDefinitionCreationScopeId, "typed", ct);

        var id = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var workflowVersionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var ownedProcessGroup = await CreationScopeGuard.StageOwnedGroupAsync(
                Security, id, $"Process definition: {request.name}", catalog, true, now, actor, token);
            // All entities are staged atomically. Their RBAC groups inherit from the process group.
            var model = new ProcessDefinitionModel(
                id, request.name, request.description, initiatorRoleTypeId,
                id, now, now, actor, actor)
                { form_definition_id = formId, workflow_definition_id = workflowId };
            // Trusted child provisioning after authorizing the existing process creation scope.
            // Newly-created scopes do not yet have EffectiveAccess projection rows.
            await _db.StageCreateAsync(new GroupModel(formId, $"Form: {request.name}", ownedProcessGroup.group_type_id,
                id, $"{ownedProcessGroup.path}{formId:D}/", true, formId, now, now, actor, actor), token);
            await _db.StageCreateAsync(new GroupModel(workflowId, $"Workflow: {request.name}", ownedProcessGroup.group_type_id,
                id, $"{ownedProcessGroup.path}{workflowId:D}/", true, workflowId, now, now, actor, actor), token);
            await _db.StageCreateAsync(new FormDefinitionModel(formId, request.name, request.description, formId, now, now, actor, actor), token);
            await _db.StageCreateAsync(new WorkflowDefinitionModel(workflowId, request.name, request.description, workflowId, now, now, actor, actor), token);
            await _db.StageCreateAsync(new FormVersionModel(formVersionId, formId, 1, false, null, formId, now, now, actor, actor), token);
            await _db.StageCreateAsync(new WorkflowVersionModel(workflowVersionId, workflowId, 1, false, null, workflowId, now, now, actor, actor), token);
            await FormVersionDefaultContent.StageAsync(_db, formVersionId, formId, now, actor, token);
            await WorkflowVersionDefaultContent.StageAsync(_db, workflowVersionId, workflowId, now, actor, token);
            await _db.StageCreateAsync(new ProcessDefinitionVersionModel(
                Guid.NewGuid(), id, formVersionId, workflowVersionId, 1, false, null,
                id, now, now, actor, actor), token);
            var created = await _db.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            await _securityProjection.SynchronizeRbacObjectAsync(id, token);
            await _securityProjection.SynchronizeRbacObjectAsync(formId, token);
            await _securityProjection.SynchronizeRbacObjectAsync(workflowId, token);
            return ProcessDefinitionResponse.FromModel(created);
        }, ct);
    }
}
