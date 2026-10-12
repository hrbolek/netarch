using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

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
) : ISecuredEntityDBModel
{
    // A process owns stable identities; its versions can select only versions of these definitions.
    // Nullable solely to permit explicit legacy-data migration.
    public Guid? form_definition_id { get; init; }
    public Guid? workflow_definition_id { get; init; }
}
