using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;

public sealed record SetApplicantVisibilityRequest(Guid StateId, Guid FormNodeId, bool CanRead);
