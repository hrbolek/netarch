namespace WebAppArchitecture.Base.UI.Routing;

public sealed record ApplicationNavigationItem(
    string Label,
    string? Url = null,
    IReadOnlyCollection<ApplicationNavigationItem>? Children = null,
    string? RequiresResponseModel = null,
    bool Separator = false
);
