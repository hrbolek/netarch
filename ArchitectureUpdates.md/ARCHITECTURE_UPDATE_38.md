# Architecture Update 38

## Entity lookup uses standard list + Hasura-like filter

Entity selection no longer introduces a separate lookup API. `EntityLookupSelect` calls the existing secured `list` endpoint with a Hasura-like `where` query and an optional `take` limit. The list resolver translates the filter to an `IQueryable` predicate and applies `Take` before materialization.

Example:

```text
GET /architecture/UserResponse/list?where={"_or":[{"Name":{"_ilike":"%nov%"}},{"surname":{"_ilike":"%nov%"}},{"email":{"_ilike":"%nov%"}}]}&take=20
Accept: application/json
```

`EntityLookupSelect.razor` is a reusable Base UI component. It renders a search input and a real `<select>` used by the submitted form. A small global delegated JavaScript handler performs the delayed query after the configured minimum number of characters.

## Role assignment dialog

The role dialog now uses correct Bootstrap modal composition: the lazy-loaded fragment itself is the `modal-content` form and contains `modal-header`, `modal-body`, and `modal-footer`. Users and role types are no longer loaded eagerly when the dialog opens. They are searched incrementally from `UserResponse/list` and `RoleTypeResponse/list`.

## List limit

Standard list endpoints accept optional `take` (1..200). With `where`, both filtering and limiting happen before DB materialization.
