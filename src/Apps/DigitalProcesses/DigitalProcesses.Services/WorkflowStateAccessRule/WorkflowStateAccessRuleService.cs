using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowStateAccessRule;
/// <summary>
/// Služba Workflow State Access Rule Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public sealed class WorkflowStateAccessRuleService(IAuthorizationLayer security, IDBLayer unsecuredDb)
    : SecuredEntityService<WorkflowStateAccessRuleModel>(security, unsecuredDb), IWorkflowStateAccessRuleService
{
    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: GetByIdAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<WorkflowStateAccessRuleResponse> CreateContainedAsync(CreateWorkflowStateAccessRuleRequest request, CancellationToken ct = default)
    {
        var (state, _) = await CreationScopeGuard.RequireEntityAsync<WorkflowStateModel>(
            Security, request.workflow_state_id, "contained", ct);
        var version = await Security.GetByIdAsync<WorkflowVersionModel>(state.workflow_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Workflow version not found or not editable.");
        if (version.is_published)
            throw new InvalidOperationException("Published workflow version is immutable.");

        _ = await Security.GetByIdAsync<RoleTypeModel>(request.source_role_type_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Source role type not found.");
        _ = await Security.GetByIdAsync<RoleTypeModel>(request.target_role_type_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Target role type not found.");

        var scope = request.target_scope.Trim().ToLowerInvariant();
        if (scope is not (WorkflowAccessTargetScopes.Request or WorkflowAccessTargetScopes.Submission or WorkflowAccessTargetScopes.FormNode))
            throw new ArgumentOutOfRangeException(nameof(request.target_scope));

        if (scope == WorkflowAccessTargetScopes.FormNode)
        {
            if (request.target_form_node_id is not Guid formNodeId)
                throw new InvalidOperationException("form-node target requires target_form_node_id.");
            var node = await Security.GetByIdAsync<FormNodeModel>(formNodeId, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Target form node not found.");
            if (!node.own_security_scope)
                throw new InvalidOperationException("A node-specific access rule requires FormNode.own_security_scope=true.");
        }
        else if (request.target_form_node_id is not null)
        {
            throw new InvalidOperationException("target_form_node_id is only valid for form-node scope.");
        }

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new WorkflowStateAccessRuleModel(
            Guid.NewGuid(), state.id, request.source_role_type_id, request.target_role_type_id,
            scope, request.target_form_node_id, state.rbacobject_id, now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var created = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return WorkflowStateAccessRuleResponse.FromModel(created);
        }, ct);
    }
}
