# Architecture Update 22

- Restored the permissions UI endpoint on the current SecurityLayer model:
  `/architecture/RbacPermissionResponse/permissions/{rbacobjectId}`.
- The endpoint no longer depends on legacy `SqliteRbacService` / legacy RBAC tables.
- Added `ISecurityPermissionQueryService` / `SecurityPermissionQueryService` over:
  `EffectiveAccess`, `Role`, `RoleType`, `RoleTypeOperation`, `Operation`, `User`, and `Group`.
- Permission inspection is itself authorized with the normal current `read` permission.
- Runtime `SecurityLayer.Query<TEntity>()` is unchanged.
- Existing `EntityPermissions.razor` URL remains compatible.
