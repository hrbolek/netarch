# Architecture Update 42 — typed vector pages, lazy HTMX vectors, permalinkable query state

This update turns the relation pages discussed after Update 41 into a typed UI composition while keeping the existing UI vocabulary (`EntityLink`, `MediumCard`, `LargeCard`, etc.) intact.

## 1. Typed composition at endpoint mapping

The standard User Membership page is now mapped as:

```csharp
.ApplyViewModel<
    UserVectorPage<UserMembershipVectorContent<TableVectorContent>>,
    EntityRelationsViewModel<UserResponse, MembershipResponse>>()
```

The type reads from the outside inward:

- `UserVectorPage<...>` — page context around one User,
- `UserMembershipVectorContent<...>` — semantic/domain vector,
- `TableVectorContent` — presentation of the vector.

The same pattern is used for User Roles and Group relation pages.

An alternative URL demonstrates that presentation is replaceable without changing the resolver or data contract:

```text
/architecture/UserResponse/memberships/{id}
/architecture/UserResponse/membershipscards/{id}
```

The first uses `TableVectorContent`, the second `CardVectorContent`.
The standard navigation continues to point to the table version.

## 2. URI composition remains unchanged

Routes continue to follow:

```text
/{app}/{typeresponse}/{operation}/{id}
```

Presentation alternatives are expressed through `operation`, not through a new route shape:

```text
/architecture/UserResponse/memberships/{id}
/architecture/UserResponse/membershipscards/{id}
```

## 3. Vector state is encoded in query parameters

Supported vector query state:

```text
where
order_by
take
skip
```

Example permalink:

```text
/architecture/UserResponse/memberships/550e8400-e29b-41d4-a716-446655440000
?where={"valid_to":{"_is_null":true}}
&order_by={"valid_from":"desc"}
&take=50
&skip=0
```

`where` is the existing Hasura-like predicate.
`order_by` is a Hasura-like JSON object with `asc`/`desc` values.

The mandatory relation predicate is always combined with the user filter by logical AND. A User membership page therefore always retains:

```text
membership.user_id == page user id
```

regardless of the supplied `where` query parameter.

## 4. Same URL for full page and HTMX fragment

No `/content` or `/fragment` route is introduced.

A normal request to:

```text
/architecture/UserResponse/memberships/{id}?...
```

returns the page shell. The vector area contains `LazyContent`, which calls the exact same URL with `HX-Request: true`.

`ApplyViewModel` already maps `HX-Request: true` to fragment mode (`IsFragment=true`). Consequently:

```text
normal request
    -> UserVectorPage<...>
       -> Page
       -> UserLargeCard
       -> filter controls
       -> LazyContent

HTMX request to the same URI
    -> UserVectorPage<...> with IsFragment=true
       -> UserMembershipVectorContent<...>
       -> TableVectorContent / CardVectorContent
```

JSON requests still load the vector immediately; lazy loading applies to the HTML page representation.

## 5. Permalink-aware filter form

`VectorQueryForm` submits with HTMX to the same path:

```text
hx-get = current path
hx-target = vector container
hx-push-url = true
```

This means applying a filter:

1. refreshes only the vector,
2. updates the browser URL,
3. produces a reloadable/shareable permalink.

The filter state is not stored in hidden client state.

## 6. Shared vector presentations

New Base UI presentations:

```text
VectorContent
CardVectorContent
CompactCardVectorContent
TableVectorContent
```

They consume `VectorContentViewModel`.
Domain vector components provide:

- entity type,
- actual items,
- optional item component for card rendering,
- table column definitions.

For example `UserMembershipVectorContent<TPresentation>` supplies Membership-specific columns while `TableVectorContent` remains domain-neutral.

This avoids classes such as:

```text
UserMembershipTableVectorContent
UserMembershipCardVectorContent
UserMembershipCompactCardVectorContent
```

while preserving the semantic/presentation split:

```text
UserMembershipVectorContent<TableVectorContent>
UserMembershipVectorContent<CardVectorContent>
```

## 7. Shared server-side vector query parsing

`VectorQueryParser` is used by generic `/list` endpoints and relation vectors.
`BaseService.VectorAsync` now accepts `VectorQueryOptions` so filtering, ordering, skip and take are applied to the secured `IQueryable` before DTO projection/materialization.

RBAC remains unchanged: `Security.Query<T>(operation)` is still the single runtime authorization path.

## 8. Intentionally unchanged

This update does **not** rename or redesign:

```text
EntityLink
MediumContent / editable content conventions
MediumCard
LargeCard
PermissionGate
EntityActionMenu
```

It also does not introduce many named slots. The new generic types describe large page/vector specialization rather than replacing normal component composition.

## Static validation note

The working environment used to produce this update does not contain the .NET SDK, so `dotnet build` could not be executed here. The changes were inspected statically; the authoritative compile/runtime check remains the devcontainer build.
