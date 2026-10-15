using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
public sealed record FormNodeDraftWorkspaceResponse(
    Guid target_version_id, FormNodeDraft draft, Guid selected_id,
    IReadOnlyList<FormNodeResponse> preview_nodes, IReadOnlyList<FormNodeTypeResponse> node_types,
    IReadOnlyList<FormRendererExample> examples);
