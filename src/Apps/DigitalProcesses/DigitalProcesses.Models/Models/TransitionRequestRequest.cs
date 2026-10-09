namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record TransitionRequestRequest(Guid transition_id, string? note = null);
