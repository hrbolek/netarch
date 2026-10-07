# Architecture Update 40 – contextual entity actions

This update introduces a reusable entity-action composition pattern for detail views and list/card rows.

## Base UI

- `EntityAction`, `EntityActionMenuModel` and `EntityActionKind` describe actions independently of presentation.
- `EntityActionMenu.razor` renders a Bootstrap kebab menu with grouping, disabled actions, navigation and HTMX delete support.
- `EntityActionSlot.razor` lazy-loads one server-composed action menu.
- `MediumCard` now accepts `ActionsUrl`; the same menu therefore appears in list rows/cards and in detail pages that reuse the medium card.

## Organizational entities

### Group
The menu combines:
- View
- Edit (RBAC `update`)
- Add member (RBAC `create` on the Group)
- Assign role (RBAC `create` on the Group)
- Add child group (RBAC `create` on the Group)
- Delete (RBAC `delete`)

### User
The menu combines:
- View
- Edit
- Add to group
- Assign role
- Delete

For target-dependent User actions, the action is enabled only if the acting user has `create` permission on at least one Group. The selected Group is still authoritatively checked by the create service.

### Role and Membership
Both now expose the same standard View/Edit/Delete action-menu pattern.

## Contextual create pages

`MembershipCreatePage`, `RoleCreatePage` and `GroupCreatePage` understand contextual query parameters (`group_id`, `user_id`, `master_group_id`, `return_url`). The contextual side is fixed, while the other side uses `EntityLookupSelect`. Group selectors use the standard list API with `operation=create`; no lookup endpoint was added.

GET create pages re-check create permission when a concrete Group/parent is supplied, and POST services remain the authoritative authorization boundary.

## List API

The generic list resolver accepts optional `operation`, validated against known RBAC operations. Together with `where` and `take`, this lets selectors query an RBAC-filtered collection using the existing Hasura-like list contract.
