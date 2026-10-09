namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormTreeResponse(FormVersionResponse form, IReadOnlyCollection<FormNodeResponse> nodes);
