namespace WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

public sealed record DigitalProcessCreatePageModel(
    string Title,
    string Action,
    string? ListUrl,
    IReadOnlyCollection<DigitalProcessCreateField> Fields,
    string? Description = null,
    string SubmitLabel = "Create");
