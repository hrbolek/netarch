namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateFormDefinitionRequest(string name, string? description = null, Guid? rbacobject_id = null);
public sealed record CreateFormVersionRequest(Guid form_definition_id, int version, bool publish = false, Guid? rbacobject_id = null);
public sealed record CreateFormNodeTypeRequest(
    string name,
    string renderer_key,
    bool can_have_children,
    bool can_have_value,
    bool can_repeat,
    Guid? parent_type_id = null,
    string? configuration_schema_json = null,
    Guid? rbacobject_id = null);

public sealed record CreateFormNodeRequest(
    Guid form_version_id,
    string key,
    Guid node_type_id,
    Guid? parent_id = null,
    string? label = null,
    string? description = null,
    int position = 0,
    int min_occurs = 1,
    int max_occurs = 1,
    bool own_security_scope = false,
    string? configuration_json = null,
    Guid? rbacobject_id = null);

public sealed record MoveFormNodeRequest(Guid? parent_id, int position = 0);

public sealed record CreateWorkflowDefinitionRequest(string name, string? description = null, Guid? rbacobject_id = null);
public sealed record CreateWorkflowVersionRequest(Guid workflow_definition_id, int version, bool publish = false, Guid? rbacobject_id = null);
public sealed record CreateWorkflowStateRequest(
    Guid workflow_version_id,
    string key,
    string name,
    string? description = null,
    bool is_initial = false,
    bool is_terminal = false,
    int position = 0,
    Guid? rbacobject_id = null);


public static class WorkflowAccessTargetScopes
{
    public const string Request = "request";
    public const string Submission = "submission";
    public const string FormNode = "form-node";
}

public sealed record CreateWorkflowStateAccessRuleRequest(
    Guid workflow_state_id,
    Guid source_role_type_id,
    Guid target_role_type_id,
    string target_scope,
    Guid? target_form_node_id = null,
    Guid? rbacobject_id = null);

public sealed record CreateWorkflowTransitionRequest(
    Guid workflow_version_id,
    Guid source_state_id,
    Guid target_state_id,
    string key,
    string name,
    bool note_required = false,
    string? condition_query = null,
    string? action_query = null,
    Guid? rbacobject_id = null);

public sealed record CreateProcessDefinitionRequest(
    string name,
    Guid initiator_role_type_id,
    string? description = null,
    Guid? rbacobject_id = null);
public sealed record CreateProcessDefinitionVersionRequest(
    Guid process_definition_id,
    Guid form_version_id,
    Guid workflow_version_id,
    int version,
    bool publish = false,
    Guid? rbacobject_id = null);

public sealed record StartRequestRequest(
    Guid process_definition_version_id,
    string? subject = null);

public sealed record CreateSubmissionNodeRequest(
    Guid submission_id,
    Guid form_node_id,
    Guid? parent_id = null,
    int occurrence = 0,
    int position = 0,
    string? value_json = null,
    Guid? rbacobject_id = null);

public sealed record SetSubmissionNodeValueRequest(string? value_json);
public sealed record TransitionRequestRequest(Guid transition_id, string? note = null);

public sealed record FormTreeResponse(FormVersionResponse form, IReadOnlyCollection<FormNodeResponse> nodes);
public sealed record SubmissionTreeResponse(SubmissionResponse submission, IReadOnlyCollection<SubmissionNodeResponse> nodes);
public sealed record RequestDetailResponse(
    RequestResponse request,
    WorkflowStateResponse state,
    SubmissionTreeResponse active_submission,
    IReadOnlyCollection<RequestTransitionHistoryResponse> history);
