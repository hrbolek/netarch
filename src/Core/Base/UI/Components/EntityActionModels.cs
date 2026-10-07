namespace WebAppArchitecture.Base.UI.Components;

public enum EntityActionKind
{
    Navigate,
    Update,
    Delete,
    Create,
    Relation,
    Custom
}

public sealed record EntityAction(
    string Key,
    string Label,
    string Url,
    EntityActionKind Kind = EntityActionKind.Navigate,
    bool Enabled = true,
    string? DisabledReason = null,
    string Group = "main",
    int Order = 0,
    string HttpMethod = "GET",
    string? Confirm = null,
    string? SuccessUrl = null);

public sealed record EntityActionMenuModel(
    IReadOnlyCollection<EntityAction> Actions,
    string Label = "Actions",
    bool Compact = true);
