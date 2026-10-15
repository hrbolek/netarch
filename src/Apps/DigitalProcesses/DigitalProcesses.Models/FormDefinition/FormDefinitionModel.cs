using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.FormDefinition;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Stabilní identita formuláře napříč jeho verzemi; nese společné pojmenování, metadata a RBAC objekt. Nová verze formuláře nevytváří novou definici.
/// </summary>
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
