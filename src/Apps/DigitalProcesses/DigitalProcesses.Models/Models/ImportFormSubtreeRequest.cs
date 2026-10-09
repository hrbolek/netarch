namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record ImportFormSubtreeRequest(
    Guid source_node_id,
    Guid target_form_version_id,
    Guid? target_parent_id = null,
    string? key_prefix = null,
    int position = 0);
