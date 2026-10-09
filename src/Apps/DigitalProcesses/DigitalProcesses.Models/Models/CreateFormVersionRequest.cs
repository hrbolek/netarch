namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateFormVersionRequest(Guid form_definition_id, int version, bool publish = false);
