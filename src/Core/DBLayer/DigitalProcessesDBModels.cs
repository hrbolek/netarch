using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

public sealed record FormDefinitionModel(
    Guid id,
    string name,
    string? description,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record FormVersionModel(
    Guid id,
    Guid form_definition_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record FormNodeTypeModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record FormNodeModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record WorkflowDefinitionModel(
    Guid id,
    string name,
    string? description,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record WorkflowVersionModel(
    Guid id,
    Guid workflow_definition_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record WorkflowStateModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record WorkflowTransitionModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record ProcessDefinitionModel(
    Guid id,
    string name,
    string? description,
    Guid initiator_role_type_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record ProcessDefinitionVersionModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;


public sealed record WorkflowStateAccessRuleModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;

// ============================================================
// Digital Processes - runtime models
// ============================================================

public sealed record RequestModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record SubmissionModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record SubmissionNodeModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record RequestTransitionHistoryModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;
