using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
public sealed record ApplicantMatrixPageData(
    ProcessDefinitionVersionResponse Model, bool MayEdit,
    WorkflowStateResponse[] States, FormNodeResponse[] Nodes);
