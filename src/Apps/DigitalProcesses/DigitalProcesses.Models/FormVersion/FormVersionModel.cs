using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Verzovaný návrh formuláře. Odkazuje na FormDefinition; publikovaná verze se používá pro běhové podání a nesmí se zaměnit s pracovním draftem.
/// </summary>
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
