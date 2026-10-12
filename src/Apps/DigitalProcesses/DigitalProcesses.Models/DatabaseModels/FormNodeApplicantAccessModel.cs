using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.DBLayer;

/// <summary>Explicit Applicant visibility in one workflow state, scoped to one immutable process definition version.</summary>
public sealed record FormNodeApplicantAccessModel(
    Guid id,
    Guid process_definition_version_id,
    Guid workflow_state_id,
    Guid form_node_id,
    bool can_read,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
