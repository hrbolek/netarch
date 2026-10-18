namespace WebAppArchitecture.Base.UI.Components;


public sealed record EntityActionMenuModel(
    IReadOnlyCollection<EntityAction> Actions,
    string Label = "Actions",
    bool Compact = true);
