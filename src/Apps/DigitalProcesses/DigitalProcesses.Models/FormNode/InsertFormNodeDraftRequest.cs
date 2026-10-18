namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;

public sealed record InsertFormNodeDraftRequest(
    Guid form_version_id, Guid? parent_id, int position, FormNodeDraft draft);
