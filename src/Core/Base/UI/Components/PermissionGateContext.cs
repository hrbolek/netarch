namespace WebAppArchitecture.Base.UI.Components;

/// <summary>
/// Permission state cascaded by <see cref="PermissionGate"/> to its child content.
/// The gate itself does not decide how denied content is rendered; children may
/// disable, hide, annotate, or otherwise adapt their own presentation.
/// </summary>
public sealed record PermissionGateContext(
    bool Allowed,
    string? DisabledReason = null);
