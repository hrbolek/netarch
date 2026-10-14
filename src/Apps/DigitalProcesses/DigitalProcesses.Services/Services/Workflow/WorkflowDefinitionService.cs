using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba Workflow Definition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class WorkflowDefinitionService(ISecurityLayer security, ISecurityProjectionService securityProjection, IDBLayer unsecuredDb)
    : SecuredBaseService<WorkflowDefinitionModel>(security, unsecuredDb), IWorkflowDefinitionService
{
    public async Task<WorkflowDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var definition = await Security.GetByIdAsync<WorkflowDefinitionModel>(id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Definition not found.");
        var versions = Security.Query<WorkflowVersionModel>(SecurityOperations.Read)
            .Where(v => v.workflow_definition_id == id).OrderByDescending(v => v.version)
            .Select(WorkflowVersionResponse.Projection).ToArray();
        return new(WorkflowDefinitionResponse.FromModel(definition), versions);
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CurrentUserId, ExecuteInTransactionAsync, StageCreateAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<WorkflowDefinitionResponse> CreateTypedAsync(CreateWorkflowDefinitionRequest request, CancellationToken ct = default)
    {
        var (catalog, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, RbacWellKnown.WorkflowDefinitionCreationScopeId, "typed", ct);
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            await CreationScopeGuard.StageOwnedGroupAsync(
                Security, id, $"Workflow definition: {request.name}", catalog, true, now, actor, token);
            var model = new WorkflowDefinitionModel(id, request.name, request.description, id, now, now, actor, actor);
            var created = await Security.StageCreateAsync(model, token);
            // The initial workflow draft is part of the same transaction as its definition.
            var firstVersion = new WorkflowVersionModel(
                Guid.NewGuid(), id, 1, false, null, id, now, now, actor, actor);
            await Security.StageCreateAsync(firstVersion, token);
            await DefaultVersionContent.StageWorkflowAsync(unsecuredDb, firstVersion.id, id, now, actor, token);
            await Security.SaveChangesAsync(token);
            await securityProjection.SynchronizeRbacObjectAsync(id, token);
            return WorkflowDefinitionResponse.FromModel(created);
        }, ct);
    }
}
