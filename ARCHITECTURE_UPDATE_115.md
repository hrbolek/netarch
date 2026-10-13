# ZArchitecture 115 — Host moved to Apps and test migration

## Changes

- Move the executable project from `src/Host/` to `src/Apps/Host/` while preserving its assembly name, namespaces and HTTP behavior.
- Update `Host.csproj` relative dependencies (`Core.Hosting`, `DigitalProcesses.Module`, optional Debug `JsonataPlayground`).
- Update `SignFakeArchitecture.sln`, integration-test ProjectReferences, PostgreSQL test configuration discovery, Dockerfile, scripts and README commands.
- Exclude obsolete `SqliteLegacyDateTimeOffsetReadTests.cs` and `SqliteDateTimeOffsetConversionTests.cs` from the DigitalProcesses test project even if they remain in a working directory after overlay extraction. They are not present in the v114 archive and reference the removed `ApplicationDbContext`/SQLite provider.
- Keep PostgreSQL-only integration-test helpers and existing test cases unchanged.

## Testing

Previous user build: `Core.SecurityLayer.Tests` 104/104 passed; `Host.Integration.Tests` 4/4 passed. Those are NOT results of version 115.

In this delivery environment the .NET SDK is not available, so build and tests could not be run. Recommended verification on development container:

```bash
dotnet restore SignFakeArchitecture.sln
dotnet build SignFakeArchitecture.sln --no-restore
dotnet test SignFakeArchitecture.sln --no-build
dotnet run --project src/Apps/Host/Host.csproj
```

**IMPORTANT:** Unpack in a clean directory, or delete the obsolete `src/Host/` directory. The old executable is intentionally absent from this full source release.
