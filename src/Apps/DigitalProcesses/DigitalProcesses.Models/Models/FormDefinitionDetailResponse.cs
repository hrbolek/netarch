namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormDefinitionDetailResponse(
    FormDefinitionResponse definition,
    IReadOnlyCollection<FormVersionResponse> versions);

