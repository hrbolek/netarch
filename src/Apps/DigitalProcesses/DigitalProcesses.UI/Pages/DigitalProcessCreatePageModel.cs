namespace WebAppArchitecture.DigitalProcesses.UI.Pages;

public sealed record DigitalProcessCreatePageModel(
    string Title,
    string Action,
    string? ListUrl,
    IReadOnlyCollection<DigitalProcessCreateField> Fields,
    string? Description = null);

public sealed record DigitalProcessCreateField(
    string Name,
    string Label,
    string InputType = "text",
    bool Required = true,
    string? HelpText = null,
    string? DefaultValue = null,
    bool Checked = false,
    string? Min = null,
    string? Max = null,
    int Rows = 5);
