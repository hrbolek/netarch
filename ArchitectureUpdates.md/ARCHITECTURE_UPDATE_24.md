# Architecture Update 24 — Entity route registry, visual labels, lazy entity links

## Goals

Centralize entity URL generation and make entity links reusable across Razor UI.
The standard route convention is:

`/{app}/{entity}/{operation}/{id}`

Default values are `architecture`, DTO type name, and `view`. Every part can be
registered centrally or overridden locally when rendering a link.

## Base UI routing

Added `Base/UI/Routing/EntityRouteRegistry.cs`:

- `IEntityRouteRegistry`
- `EntityRouteRegistry`
- `EntityRouteDefinition`

Resolution order is:

1. explicit Razor/component override,
2. registered entity/type override,
3. convention.

`GroupTypeResponse` demonstrates a global exception: its default operation is
`read`, because that endpoint does not currently expose `/view/{id}`.

## EntityLink

`Base/UI/Components/EntityLink.razor` now uses the central registry.

Preferred parameters:

- `App`
- `Entity`
- `Operation`
- `EntityId`

Legacy `Type` and `Action` parameters remain supported as aliases.
Without custom child content, the component renders `Model.visual_label`.

## visual_label

`IEntityDTOModel` exposes `visual_label`. Response DTOs now explicitly populate
it in both `FromModel` and `Projection`:

- User: `Name + surname`
- Group / GroupType: `Name`
- RoleType: `name`
- Membership: `user @ group`
- Role: `role type · user`

The interface has an ID fallback for DTO types where no explicit visual label is
provided (notably update request DTOs).

## Lazy links

Added:

- `EntityLinkResponse`
- `EntityLinkFragment.razor`
- `LazyEntityLink.razor`
- generic `BaseResolvers.Link<...>()`

Standard link endpoints were added for User, Group, GroupType, Membership, Role,
and RoleType:

`/{app}/{EntityResponse}/link/{id}`

The resolver returns `EntityLinkResponse`. Existing representation negotiation
returns JSON to API clients or the Razor `EntityLinkFragment` to HTML/HTMX
clients.

## Membership UI

`MembershipMediumCard.razor` now shows the raw `group_id` and `user_id` and then
loads the human-readable entity link lazily through HTMX. This avoids forcing
related entity labels into the initial card render while preserving a useful
clickable reference.

## Security permissions UI

`RbacPermissionList.razor` no longer assembles href strings itself. User, group,
role, role type and membership references are rendered via `EntityLink` and
therefore use the same central route registry.

## Database / runtime security

No database schema changes.
`SecurityLayer.Query<TEntity>()` is unchanged.
