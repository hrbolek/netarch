namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormLibraryItemResponse(
    Guid source_form_version_id,
    Guid source_node_id,
    string label,
    string key,
    string renderer_key);
