namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormDesignerResponse(
    FormVersionResponse form,
    IReadOnlyCollection<FormNodeResponse> nodes,
    IReadOnlyCollection<FormNodeTypeResponse> node_types,
    IReadOnlyCollection<FormLibraryItemResponse> library_items);
