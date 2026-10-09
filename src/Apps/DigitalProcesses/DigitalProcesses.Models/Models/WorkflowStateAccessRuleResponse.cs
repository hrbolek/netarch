using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record WorkflowStateAccessRuleResponse(
    Guid id,
    Guid workflow_state_id,
    Guid source_role_type_id,
    Guid target_role_type_id,
    string target_scope,
    Guid? target_form_node_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<WorkflowStateAccessRuleModel, WorkflowStateAccessRuleResponse>, ISecuredEntityDTOModel
{
    public static WorkflowStateAccessRuleResponse FromModel(WorkflowStateAccessRuleModel model) => new(model.id, model.workflow_state_id, model.source_role_type_id, model.target_role_type_id, model.target_scope, model.target_form_node_id, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"{model.target_scope}: {model.source_role_type_id} -> {model.target_role_type_id}");
    public static Expression<Func<WorkflowStateAccessRuleModel, WorkflowStateAccessRuleResponse>> Projection => model => new(model.id, model.workflow_state_id, model.source_role_type_id, model.target_role_type_id, model.target_scope, model.target_form_node_id, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.target_scope);
    public WorkflowStateAccessRuleModel ToModel() => new(id, workflow_state_id, source_role_type_id, target_role_type_id, target_scope, target_form_node_id, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
