using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.UI.Pages;

public sealed record ProcessVersionTestPageModel(
    ProcessDefinitionVersionResponse ProcessVersion,
    RequestDetailResponse Request,
    FormDesignerResponse Form,
    WorkflowDesignerResponse Workflow);
