using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormDefinitionResponse(
    Guid id,
    string name,
    string? description,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<FormDefinitionModel, FormDefinitionResponse>, ISecuredEntityDTOModel
{
    public static FormDefinitionResponse FromModel(FormDefinitionModel model) => new(model.id, model.name, model.description, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<FormDefinitionModel, FormDefinitionResponse>> Projection => model => new(model.id, model.name, model.description, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public FormDefinitionModel ToModel() => new(id, name, description, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record FormVersionResponse(
    Guid id,
    Guid form_definition_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<FormVersionModel, FormVersionResponse>, ISecuredEntityDTOModel
{
    public static FormVersionResponse FromModel(FormVersionModel model) => new(model.id, model.form_definition_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Form version {model.version}");
    public static Expression<Func<FormVersionModel, FormVersionResponse>> Projection => model => new(model.id, model.form_definition_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Form version");
    public FormVersionModel ToModel() => new(id, form_definition_id, version, is_published, published_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record FormNodeTypeResponse(
    Guid id,
    Guid? parent_type_id,
    string name,
    string renderer_key,
    bool can_have_children,
    bool can_have_value,
    bool can_repeat,
    string? configuration_schema_json,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<FormNodeTypeModel, FormNodeTypeResponse>, ISecuredEntityDTOModel
{
    public static FormNodeTypeResponse FromModel(FormNodeTypeModel model) => new(model.id, model.parent_type_id, model.name, model.renderer_key, model.can_have_children, model.can_have_value, model.can_repeat, model.configuration_schema_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<FormNodeTypeModel, FormNodeTypeResponse>> Projection => model => new(model.id, model.parent_type_id, model.name, model.renderer_key, model.can_have_children, model.can_have_value, model.can_repeat, model.configuration_schema_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public FormNodeTypeModel ToModel() => new(id, parent_type_id, name, renderer_key, can_have_children, can_have_value, can_repeat, configuration_schema_json, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record FormNodeResponse(
    Guid id,
    Guid form_version_id,
    Guid? parent_id,
    string path,
    string key,
    string? label,
    string? description,
    Guid node_type_id,
    int position,
    int min_occurs,
    int max_occurs,
    bool own_security_scope,
    string? configuration_json,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<FormNodeModel, FormNodeResponse>, ISecuredEntityDTOModel
{
    public static FormNodeResponse FromModel(FormNodeModel model) => new(model.id, model.form_version_id, model.parent_id, model.path, model.key, model.label, model.description, model.node_type_id, model.position, model.min_occurs, model.max_occurs, model.own_security_scope, model.configuration_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, (model.label ?? model.key));
    public static Expression<Func<FormNodeModel, FormNodeResponse>> Projection => model => new(model.id, model.form_version_id, model.parent_id, model.path, model.key, model.label, model.description, model.node_type_id, model.position, model.min_occurs, model.max_occurs, model.own_security_scope, model.configuration_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, (model.label ?? model.key));
    public FormNodeModel ToModel() => new(id, form_version_id, parent_id, path, key, label, description, node_type_id, position, min_occurs, max_occurs, own_security_scope, configuration_json, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record WorkflowDefinitionResponse(
    Guid id,
    string name,
    string? description,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<WorkflowDefinitionModel, WorkflowDefinitionResponse>, ISecuredEntityDTOModel
{
    public static WorkflowDefinitionResponse FromModel(WorkflowDefinitionModel model) => new(model.id, model.name, model.description, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<WorkflowDefinitionModel, WorkflowDefinitionResponse>> Projection => model => new(model.id, model.name, model.description, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public WorkflowDefinitionModel ToModel() => new(id, name, description, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record WorkflowVersionResponse(
    Guid id,
    Guid workflow_definition_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<WorkflowVersionModel, WorkflowVersionResponse>, ISecuredEntityDTOModel
{
    public static WorkflowVersionResponse FromModel(WorkflowVersionModel model) => new(model.id, model.workflow_definition_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Workflow version {model.version}");
    public static Expression<Func<WorkflowVersionModel, WorkflowVersionResponse>> Projection => model => new(model.id, model.workflow_definition_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Workflow version");
    public WorkflowVersionModel ToModel() => new(id, workflow_definition_id, version, is_published, published_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record WorkflowStateResponse(
    Guid id,
    Guid workflow_version_id,
    string key,
    string name,
    string? description,
    bool is_initial,
    bool is_terminal,
    int position,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<WorkflowStateModel, WorkflowStateResponse>, ISecuredEntityDTOModel
{
    public static WorkflowStateResponse FromModel(WorkflowStateModel model) => new(model.id, model.workflow_version_id, model.key, model.name, model.description, model.is_initial, model.is_terminal, model.position, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<WorkflowStateModel, WorkflowStateResponse>> Projection => model => new(model.id, model.workflow_version_id, model.key, model.name, model.description, model.is_initial, model.is_terminal, model.position, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public WorkflowStateModel ToModel() => new(id, workflow_version_id, key, name, description, is_initial, is_terminal, position, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

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

public sealed record WorkflowTransitionResponse(
    Guid id,
    Guid workflow_version_id,
    Guid source_state_id,
    Guid target_state_id,
    string key,
    string name,
    bool note_required,
    string? condition_query,
    string? action_query,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<WorkflowTransitionModel, WorkflowTransitionResponse>, ISecuredEntityDTOModel
{
    public static WorkflowTransitionResponse FromModel(WorkflowTransitionModel model) => new(model.id, model.workflow_version_id, model.source_state_id, model.target_state_id, model.key, model.name, model.note_required, model.condition_query, model.action_query, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<WorkflowTransitionModel, WorkflowTransitionResponse>> Projection => model => new(model.id, model.workflow_version_id, model.source_state_id, model.target_state_id, model.key, model.name, model.note_required, model.condition_query, model.action_query, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public WorkflowTransitionModel ToModel() => new(id, workflow_version_id, source_state_id, target_state_id, key, name, note_required, condition_query, action_query, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record ProcessDefinitionResponse(
    Guid id,
    string name,
    string? description,
    Guid initiator_role_type_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<ProcessDefinitionModel, ProcessDefinitionResponse>, ISecuredEntityDTOModel
{
    public static ProcessDefinitionResponse FromModel(ProcessDefinitionModel model) => new(model.id, model.name, model.description, model.initiator_role_type_id, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<ProcessDefinitionModel, ProcessDefinitionResponse>> Projection => model => new(model.id, model.name, model.description, model.initiator_role_type_id, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public ProcessDefinitionModel ToModel() => new(id, name, description, initiator_role_type_id, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record ProcessDefinitionVersionResponse(
    Guid id,
    Guid process_definition_id,
    Guid form_version_id,
    Guid workflow_version_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<ProcessDefinitionVersionModel, ProcessDefinitionVersionResponse>, ISecuredEntityDTOModel
{
    public static ProcessDefinitionVersionResponse FromModel(ProcessDefinitionVersionModel model) => new(model.id, model.process_definition_id, model.form_version_id, model.workflow_version_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Process version {model.version}");
    public static Expression<Func<ProcessDefinitionVersionModel, ProcessDefinitionVersionResponse>> Projection => model => new(model.id, model.process_definition_id, model.form_version_id, model.workflow_version_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Process version");
    public ProcessDefinitionVersionModel ToModel() => new(id, process_definition_id, form_version_id, workflow_version_id, version, is_published, published_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record RequestResponse(
    Guid id,
    Guid process_definition_version_id,
    Guid initiator_user_id,
    Guid current_state_id,
    Guid current_submission_id,
    string? subject,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<RequestModel, RequestResponse>, ISecuredEntityDTOModel
{
    public static RequestResponse FromModel(RequestModel model) => new(model.id, model.process_definition_version_id, model.initiator_user_id, model.current_state_id, model.current_submission_id, model.subject, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, (model.subject ?? $"Request {model.id}"));
    public static Expression<Func<RequestModel, RequestResponse>> Projection => model => new(model.id, model.process_definition_version_id, model.initiator_user_id, model.current_state_id, model.current_submission_id, model.subject, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, (model.subject ?? "Request"));
    public RequestModel ToModel() => new(id, process_definition_version_id, initiator_user_id, current_state_id, current_submission_id, subject, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record SubmissionResponse(
    Guid id,
    Guid request_id,
    Guid form_version_id,
    int version,
    bool is_archived,
    DateTimeOffset? archived_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<SubmissionModel, SubmissionResponse>, ISecuredEntityDTOModel
{
    public static SubmissionResponse FromModel(SubmissionModel model) => new(model.id, model.request_id, model.form_version_id, model.version, model.is_archived, model.archived_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Submission v{model.version}");
    public static Expression<Func<SubmissionModel, SubmissionResponse>> Projection => model => new(model.id, model.request_id, model.form_version_id, model.version, model.is_archived, model.archived_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Submission");
    public SubmissionModel ToModel() => new(id, request_id, form_version_id, version, is_archived, archived_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record SubmissionNodeResponse(
    Guid id,
    Guid submission_id,
    Guid form_node_id,
    Guid? parent_id,
    string path,
    int occurrence,
    int position,
    string? value_json,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<SubmissionNodeModel, SubmissionNodeResponse>, ISecuredEntityDTOModel
{
    public static SubmissionNodeResponse FromModel(SubmissionNodeModel model) => new(model.id, model.submission_id, model.form_node_id, model.parent_id, model.path, model.occurrence, model.position, model.value_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Node {model.occurrence}");
    public static Expression<Func<SubmissionNodeModel, SubmissionNodeResponse>> Projection => model => new(model.id, model.submission_id, model.form_node_id, model.parent_id, model.path, model.occurrence, model.position, model.value_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Node");
    public SubmissionNodeModel ToModel() => new(id, submission_id, form_node_id, parent_id, path, occurrence, position, value_json, rbacobject_id, created_at, updated_at, created_by, updated_by);
}

public sealed record RequestTransitionHistoryResponse(
    Guid id,
    Guid request_id,
    Guid transition_id,
    Guid from_state_id,
    Guid to_state_id,
    Guid archived_submission_id,
    Guid actor_user_id,
    string? note,
    DateTimeOffset changed_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<RequestTransitionHistoryModel, RequestTransitionHistoryResponse>, ISecuredEntityDTOModel
{
    public static RequestTransitionHistoryResponse FromModel(RequestTransitionHistoryModel model) => new(model.id, model.request_id, model.transition_id, model.from_state_id, model.to_state_id, model.archived_submission_id, model.actor_user_id, model.note, model.changed_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Transition {model.changed_at:u}");
    public static Expression<Func<RequestTransitionHistoryModel, RequestTransitionHistoryResponse>> Projection => model => new(model.id, model.request_id, model.transition_id, model.from_state_id, model.to_state_id, model.archived_submission_id, model.actor_user_id, model.note, model.changed_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Transition");
    public RequestTransitionHistoryModel ToModel() => new(id, request_id, transition_id, from_state_id, to_state_id, archived_submission_id, actor_user_id, note, changed_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
