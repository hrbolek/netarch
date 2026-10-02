namespace WebAppArchitecture.Base.UI.Components;

public sealed record EntityNavigationLink(
    string Label,
    string Href,
    string? Description = null
);
