using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Typ Workflow Access Target Scopes ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public static class WorkflowAccessTargetScopes
{
    public const string Request = "request";
    public const string Submission = "submission";
    public const string FormNode = "form-node";
}
