# Architecture Update 45

This update hardens secured vector/list rendering and paging without changing the established route grammar or the MediumCard/LargeCard/EntityLink vocabulary.

## Bounded HTTP vectors

`VectorQueryParser` now defines:

- `DefaultTake = 25`
- `MaxTake = 200`

When an HTTP list/vector request omits `take`, the effective page size is 25. This applies to standard `/list` endpoints and relation-vector pages. Internal service calls that do not go through the HTTP parser remain unbounded unless they explicitly pass a limit.

## Stable ordering

`BaseService` now applies deterministic ordering to vector queries:

- no `order_by` -> `id ASC`
- explicit `order_by` -> requested fields plus `id ASC` as a final tie breaker, unless `id` was already supplied

`VectorQueryParser` also rejects `[NotMapped]` fields in `order_by` because those fields cannot be ordered by the database query.

## Paging without COUNT(*)

`VectorPageAsync` probes one extra row (`take + 1`). The extra row is not returned to the client; it is used only to determine `HasMore`.

The standard list JSON/HTML model remains the same DTO array for compatibility. Paging metadata is emitted through:

- `X-Vector-Take`
- `X-Vector-Skip`
- `X-Vector-Has-More`

Relation pages carry the same information in `EntityRelationsViewModel.PageInfo`.

`VectorPager` renders Previous/Next links. Relation pages use HTMX against the same canonical route and preserve `where`, `order_by`, and `take` in the URI. `hx-push-url=true` keeps the current page state permalinkable.

Changing filter/order/page size submits `skip=0`, so paging restarts from the beginning.

## Empty states

Vector renderers now distinguish:

- no items
- no items matching the current Hasura-like filter

## Secured relation projections

`MembershipService` and `RoleService` override vector queries with enriched, secured projections.

Membership requires readable:

- Membership
- User
- Group

Role requires readable:

- Role
- User
- Group
- RoleType

All participating secured entities enter the LINQ query through `Security.Query<TEntity>(operation)`. Therefore a relation row is visible only when the current actor can read the neighbouring entities needed to render it.

The projection produces display fields in one SQL query:

- Membership: `UserName @ GroupName`
- Role: `UserName @ GroupName [RoleTypeName]`

These are read-model fields only; the database is not denormalized.

The relation labels and MediumCards use these projected names directly as links to neighbouring entities. `LazyEntityLink` remains a fallback for models obtained outside an enriched vector projection, avoiding N+1 HTTP requests in list/vector rendering while retaining robustness for standalone detail models.

## Compatibility

- canonical routes remain unchanged
- short-route redirects from Update 44 remain unchanged
- standard `/list` response models remain DTO arrays
- existing service `VectorAsync` remains available
- cursor paging is intentionally deferred; current paging remains `skip`/`take`

## Build note

The environment used to prepare this update does not contain the .NET SDK, so the source was statically inspected but not compiled here.
