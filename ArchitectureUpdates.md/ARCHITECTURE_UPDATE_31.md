# Architecture Update 31

## Database dump lifecycle

The Host now restores `./data/dump.json` on startup when the file exists. The path can be overridden by `DatabaseDump:Path` or `DATABASE_DUMP_PATH`.

The dump is a JSON snapshot of SQLite physical table rows rather than JSON serialization of EF entity objects. This avoids coupling the snapshot format to record constructors or navigation properties.

Startup order:

1. EF Core creates the current schema with `EnsureCreatedAsync`.
2. `dump.json` is restored when present.
3. `DatabaseInitializer` runs idempotent bootstrap checks. SystemData import is skipped after a successful restore.
4. `EffectiveAccess` is rebuilt.

`DatabaseDumpHostedService.StopAsync` writes a new atomic dump during graceful ASP.NET Core shutdown. This includes Ctrl+C and SIGTERM handled by the Generic Host.

## Creating a role from Valid Permissions

`EntityPermissions.razor` now has an **Add role** button in the card header. It opens a Bootstrap modal whose content is lazy-loaded.

The dialog contains:

- secured User `<select>`,
- secured RoleType `<select>`,
- `valid_from`,
- optional `valid_to`,
- deputy flag.

The POST endpoint verifies the current actor has `create` on the target `rbacobject_id`, creates the ordinary `RoleModel`, rebuilds `EffectiveAccess` through the existing `RoleService`, and redirects back to the page that opened the dialog.

Endpoints:

- `GET /architecture/RbacPermissionResponse/create-role/{rbacobjectId}`
- `POST /architecture/RbacPermissionResponse/create-role-form/{rbacobjectId}`

No second authorization mechanism was introduced.

## Hasura-like `where` filters on list endpoints

All endpoints using `BaseResolvers.List` now optionally accept a `where` query parameter containing a JSON object in the same format consumed by `HasuraFilterExpression`.

Example:

```text
/architecture/UserResponse/list?where={"Name":{"_ilike":"%Novak%"}}
```

Conceptually:

```csharp
var predicate = HasuraFilterExpression.Build<TDBModel>(where);
var result = await service.VectorAsync(predicate, RbacOperations.List, cancellationToken);
```

The predicate is therefore applied to the secured `IQueryable<TDBModel>` before projection/materialization, so filtering is translated into the database SELECT rather than performed over an in-memory collection.

`PageViewList.razor` and `DigitalProcessListPage.razor` now expose an optional Hasura `where` filter input. It is enabled by default and can be disabled with `ShowFilter="false"`.

The existing User vector request continues to use the same `HasuraFilterExpression` and its body property is explicitly documented as JSON key `where`.
