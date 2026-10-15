using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
namespace WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
/// <summary>
/// Výstupní DTO entity Form Definition Detail response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record FormDefinitionDetailResponse(
    FormDefinitionResponse definition,
    IReadOnlyCollection<FormVersionResponse> versions);
