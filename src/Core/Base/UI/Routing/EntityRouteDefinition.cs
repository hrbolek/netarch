namespace WebAppArchitecture.Base.UI.Routing;

public sealed record EntityRouteDefinition(
    string App,
    string Entity,
    string Operation = "view");
