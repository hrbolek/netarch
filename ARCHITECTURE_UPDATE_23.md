# Architecture Update 23

Security permissions UI now follows the same application layering as User.

## New projects

- `Security.Models` — `RbacPermissionResponse` and item DTOs.
- `Security.Services` — read-only permissions query service over DBLayer.
- `Security.UI` — Razor components/pages that render permissions.
- `Security.Resolvers` — Minimal API resolvers and endpoint mapping.

## Content negotiation

The existing compatible endpoint remains:

`GET /architecture/RbacPermissionResponse/permissions/{rbacobjectId}`

It is mapped with `ApplyViewModel<RbacPermissionView, RbacPermissionResponse>()`:

- `Accept: application/json` -> JSON DTO.
- `Accept: text/html` -> Razor HTML.
- `HX-Request: true` -> Razor fragment suitable for `LazyContent`.

No HTML is assembled manually in C#.

## Runtime security

`SecurityLayer.Query<TEntity>()` is unchanged. The runtime SecurityLayer remains infrastructure only; UI/query presentation has moved out of it.
