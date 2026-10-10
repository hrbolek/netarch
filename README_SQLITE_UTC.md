# ZArchitecture78 – SQLite DateTimeOffset UTC conversion

Apply in `/workspace` (the archive has project-relative paths):

```bash
unzip -o ZArchitecture78_sqlite_utc_datetimeoffset_patch.zip -d /workspace
```

Changed files:

- `src/Core/DBLayer/ApplicationDbContext.cs`: for SQLite only, convert all `DateTimeOffset` and `DateTimeOffset?` values to uniform UTC ISO-8601 (`"O"`) text. Other providers preserve their original mappings.
- `src/Core/SecurityLayer/RbacAccessBindingManager.cs`: restores fully SQL-translated LINQ predicate (removes `AsEnumerable` workaround).
- `tests/Apps/DigitalProcesses.Integration.Tests/SqliteDateTimeOffsetConversionTests.cs`: two tests for UTC equivalence and SQLite query translation.

Run:

```bash
dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug --filter 'FullyQualifiedName~DP27_|FullyQualifiedName~DP28_'
dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug
```

**Compatibility:** Previously persisted SQLite values may use an older storage representation (especially mixed offsets). On populated SQLite databases, migrate the existing DateTimeOffset columns to canonical UTC text before adopting this converter. New isolated SQLite test databases need no data migration. No PostgreSQL or SQL Server mappings are changed.

**Verification:** Static patch checks passed. .NET build/test cannot be executed in this environment.
