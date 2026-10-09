namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateSubmissionNodeRequest(
    Guid submission_id,
    Guid form_node_id,
    Guid? parent_id = null,
    int occurrence = 0,
    int position = 0,
    string? value_json = null);
