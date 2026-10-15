using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;
/// <summary>Public, already authorized data available to local form expressions.
/// Callers must filter field values and history using the effective read policy before building.</summary>
public sealed record FormContextMetadata(Guid? RequestId = null, Guid? SubmissionId = null,
    Guid? WorkflowVersionId = null, Guid? CurrentStateId = null, string? CurrentStateName = null);
