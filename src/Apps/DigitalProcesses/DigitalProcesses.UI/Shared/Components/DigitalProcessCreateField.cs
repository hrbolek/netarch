namespace WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

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
    int Rows = 5,
    IReadOnlyCollection<DigitalProcessSelectOption>? Options = null,
    string? LookupUrl = null,
    IReadOnlyCollection<string>? SearchFields = null,
    string? ParentField = null, string? ParentProperty = null);
