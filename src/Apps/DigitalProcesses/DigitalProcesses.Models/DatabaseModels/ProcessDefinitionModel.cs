using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Kořenový agregát digitálního procesu. Udržuje stabilní identitu procesu a odkazy na jedinou vlastněnou definici formuláře a workflow; jednotlivé verze volí jen jejich verze. Nullable odkazy jsou určeny pro migraci starších záznamů.
/// </summary>
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
