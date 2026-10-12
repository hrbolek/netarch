using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Form Designer response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record FormDesignerResponse(
    FormVersionResponse form,
    IReadOnlyCollection<FormNodeResponse> nodes,
    IReadOnlyCollection<FormNodeTypeResponse> node_types,
    IReadOnlyCollection<FormLibraryItemResponse> library_items);
