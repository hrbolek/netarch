using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Stabilní identita workflow; jeho verze obsahují stavový automat, který řídí postup žádostí.
/// </summary>
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
