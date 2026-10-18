# Architecture Update 32

## Fix: role dialog ordering and EF Core translation

`SecurityResolvers.CreateRoleDialog` previously ordered `IUserService.Query()` and
`IRoleTypeService.Query()` by the DTO property `visual_label` while the query was
still an `IQueryable`. EF Core attempted to translate ordering through the DTO
constructor projection and failed.

The dialog now projects the secured DTO data first, materializes the small option
set, and only then sorts the `RoleAssignmentOption` objects in memory. This does
not change `Security.Query<TEntity>()`, the RBAC projection, or general list/vector
query behavior.

Also adds the missing `Microsoft.Extensions.DependencyInjection` using required by
`GetRequiredService<T>()` in the same resolver file.
