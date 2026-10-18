namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;

public sealed record FormNodeDraftWorkspaceRequest(
    Guid target_version_id, FormNodeDraft draft, Guid? selected_id = null);
