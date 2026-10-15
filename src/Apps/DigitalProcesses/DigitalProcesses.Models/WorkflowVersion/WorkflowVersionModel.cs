using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Konkrétní verze definice workflow. Stavy a přechody patří k této verzi; proces vybírá explicitní workflow verzi.
/// </summary>
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
