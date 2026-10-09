namespace WebAppArchitecture.Base.UI.Routing;

public sealed record ApplicationDefinition(
    string Key,
    string Label,
    string RootUrl,
    string Description,
    IReadOnlyCollection<ApplicationNavigationItem> Navigation
);
