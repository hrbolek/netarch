using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowState;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Stav stavového automatu jedné verze workflow, na který mohou odkazovat pravidla přístupu, přechody a běžící žádosti.
/// </summary>
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
