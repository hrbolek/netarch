using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;

namespace WebAppArchitecture.DigitalProcesses.Models.Request;

public sealed record RequestWorkspaceResponse(
    RequestDetailResponse Request,
    FormDesignerResponse Form,
    WorkflowDesignerResponse Workflow,
    IReadOnlyCollection<Guid> EditableNodeIds,
    bool MayTransition);
