using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;
/// <summary>One immutable transition: StateId is the state BEFORE transition, and
/// ArchivedSubmissionId is the snapshot of that state. StateName and ActorEmail
/// are resolved display properties, not additional stored historical identifiers.</summary>
public sealed record FormContextHistoryEntry(Guid StateId, string? StateName,
    Guid ActorUserId, string? ActorEmail, string? PublicNote,
    DateTimeOffset ChangedAt, Guid ArchivedSubmissionId);
