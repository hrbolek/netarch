namespace WebAppArchitecture.DigitalProcesses.UI.Pages;
public sealed record DefinitionVersionItem(Guid Id, int Number, bool Published, DateTimeOffset Updated);
public sealed record DefinitionVersionsPageModel(Guid Id, string Name, string Kind, IReadOnlyList<DefinitionVersionItem> Versions);
