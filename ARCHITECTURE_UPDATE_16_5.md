# Architecture Update 16.5

- `EffectiveAccessModel` stores validity as Unix milliseconds (`long` / `long?`).
- This avoids SQLite/EF Core translation failures for `DateTimeOffset <=/>=` comparisons.
- Security `IQueryable` filters remain server-side and composable.
- `CanAssignDeputyRoleAsync` narrows candidates in SQL and evaluates DateTimeOffset validity in memory.
- Because the `effective_access` schema changed, recreate the prototype SQLite database (or add a migration).
