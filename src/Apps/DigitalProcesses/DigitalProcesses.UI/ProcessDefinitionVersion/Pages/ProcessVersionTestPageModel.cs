using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;

namespace WebAppArchitecture.DigitalProcesses.UI.ProcessDefinitionVersion.Pages;

public sealed record ProcessVersionTestPageModel(
    ProcessDefinitionVersionResponse ProcessVersion,
    RequestDetailResponse Request,
    FormDesignerResponse Form,
    WorkflowDesignerResponse Workflow);
