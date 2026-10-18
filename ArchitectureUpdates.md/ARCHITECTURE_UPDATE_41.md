# Architecture Update 41 — PermissionGate composition

This update intentionally keeps the existing visualization vocabulary and hierarchy (`EntityLink`, `MediumContent`, `MediumEditableContent`, `MediumCard`, `LargeCard`). It does not introduce a new generic entity-view framework or a multi-slot layout system.

## PermissionGate

`Core/Base/UI/Components/PermissionGate.razor` is a small compositional wrapper with a single `ChildContent` slot. It cascades `PermissionGateContext` to child components.

It supports two authorization modes:

1. **Precomputed capability** — `Allowed` is supplied by a resolver. This is used by entity action menus so several permissions can be evaluated together server-side without issuing one database query per menu item.
2. **Direct RBAC evaluation** — `RbacObjectId` and `Operation` are supplied. The component resolves the current acting user and calls `IRbacAuthorizationService` itself. This is useful for isolated mutation controls outside a precomputed action menu.

The gate does not decide how denied content looks. Children consume the cascading context and choose their own representation (disabled, hidden, lock indicator, etc.).

## EntityActionMenu

`EntityActionMenu` now composes each action as:

```razor
<PermissionGate Allowed="@action.Enabled" DisabledReason="@action.DisabledReason">
    <EntityActionItem Action="@action" />
</PermissionGate>
```

`EntityActionItem` consumes the cascaded permission context. Allowed actions render as links/buttons; denied actions render disabled with a lock indicator and no active URL or HTMX mutation.

This preserves defense in depth: UI gating improves usability, while GET mutation dialogs/pages and POST/DELETE endpoints retain their own authorization checks.

## Scope

No changes were made to `MediumCard`, `LargeCard`, `EntityLink`, or their naming/semantic roles. The new gate is deliberately a small Base UI primitive that can wrap existing domain mutation components.
