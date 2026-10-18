namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;

public sealed record FormNodeDraftSearchRequest(
    FormNodeDraftWorkspaceRequest workspace, Guid node_id, string pattern,
    IReadOnlyList<FormSearchValue>? values = null);
