# Architecture update 09

## Persistence

The default runtime persistence is now one SQLite database through EF Core:

`Resolver -> Service -> Store -> ApplicationDbContext -> SQLite`

The default connection string is `Data Source=./data/application.db`.

`IQueryable`, `DbSet` and `ApplicationDbContext` remain inside the Persistence/Store layer.

## Group hierarchy

`GroupModel` contains:

- `group_type_id`
- `master_group_id`
- `path` (materialized path, including the group itself)
- `rbac_id`

For a Group, the invariant is `rbac_id == id`. The duplicated value is intentional so that Group follows the same secured-object contract as all other secured entities.

Moving a group rewrites the materialized path of all descendants and prevents hierarchy cycles.

## Group type hierarchy

`GroupTypeModel` is itself hierarchical:

- `parent_group_type_id`
- `path` (materialized path)

Groups reference their type through `group_type_id`.

API endpoints are exposed under `/architecture/GroupTypeResponse`.

## Security

Authorization is represented by a dedicated `SecurityService`, separate from `ISecurityStore`.

`SecurityService` receives an RBAC id (which resolves to a Group id), while the EF-backed `SecurityStore`:

1. loads the Group materialized path,
2. parses self + ancestor Group ids,
3. finds active roles for the supplied user in those groups,
4. resolves RoleTypes,
5. returns the distinct operation names.

Endpoint:

`GET /architecture/security/{rbacId}/operations?userId={userId}`

If `userId` is omitted, the current authenticated actor is used.
