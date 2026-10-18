using WebAppArchitecture.DigitalProcesses.Models.FormNode;
namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;
/// <summary>
/// Výstupní DTO entity Form Designer Page response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>

public sealed record FormDesignerPageResponse(
    FormDesignerResponse designer, bool test_mode, Guid? root_node_id = null,
    IReadOnlyList<FormNodeResponse>? ancestor_nodes = null);
