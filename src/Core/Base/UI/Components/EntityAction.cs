namespace WebAppArchitecture.Base.UI.Components;

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
