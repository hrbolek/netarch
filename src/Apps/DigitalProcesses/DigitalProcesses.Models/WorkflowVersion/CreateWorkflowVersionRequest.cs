using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
/// <summary>
/// Vstupní kontrakt Create Workflow Version Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record CreateWorkflowVersionRequest(
    Guid workflow_definition_id, int version, bool publish = false,
    IReadOnlyList<WorkflowStateDefinition>? states = null,
    IReadOnlyList<WorkflowTransitionDefinition>? transitions = null);
