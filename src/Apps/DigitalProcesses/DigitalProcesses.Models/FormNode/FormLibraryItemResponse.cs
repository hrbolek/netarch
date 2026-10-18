namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
/// <summary>
/// Výstupní DTO entity Form Library Item response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>

public sealed record FormLibraryItemResponse(
    Guid source_form_version_id,
    Guid source_node_id,
    string label,
    string key,
    string renderer_key);
