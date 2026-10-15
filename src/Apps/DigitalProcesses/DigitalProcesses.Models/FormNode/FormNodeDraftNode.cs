namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
public sealed record FormNodeDraftNode(
    Guid id, Guid? parent_id, string key, Guid node_type_id,
    string? label = null, string? description = null, int position = 0,
    int min_occurs = 1, int max_occurs = 1, string? configuration_json = "{}");
