using WebAppArchitecture.DigitalProcesses.Models.FormNode;
namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;
/// <summary>
/// Výstupní DTO entity Form Tree response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record FormTreeResponse(FormVersionResponse form, IReadOnlyCollection<FormNodeResponse> nodes);
