namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;

public sealed record PrepareFormNodeDraftRequest(Guid? source_node_id = null, Guid? node_type_id = null, bool include_children = true);
