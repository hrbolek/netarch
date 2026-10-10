namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record StartRequestRequest(
    Guid process_definition_version_id,
    string? subject = null,
    Guid? target_group_id = null);
