namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
public sealed record FormNodeDraft(
    Guid root_id, IReadOnlyList<FormNodeDraftNode> nodes,
    IReadOnlyDictionary<Guid, string>? reference_paths = null);
