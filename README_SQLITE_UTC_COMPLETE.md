# ZArchitecture78 – complete sources (SQLite DateTimeOffset fix)

This archive is a complete source tree, not an incremental patch.
Base: ZArchitecture78_RbacBinding_DateTimeOffset_full.zip.
Changes incorporated:
- SQLite-only UTC ISO 8601 conversion for DateTimeOffset / DateTimeOffset?
- DateTimeOffset.Parse (rather than ParseExact("O")) for EF's existing SQLite values
- Retain SQL-side RBAC binding expiration filtering
- DP26 direct service lifecycle test in DP26_ServiceLifecycleTests.cs (no duplicate class)
- DateTimeOffset SQLite regression tests
- PostgreSQL/SQL Server mapping unchanged

IMPORTANT: Sources are reconstructed from the latest complete project archive available in this conversation plus subsequent incremental patches. Local workspace edits not provided here cannot be included. Back up your workspace or examine diffs before replacing it. Existing SQLite rows using mixed time formats require normalization before SQL lexical comparisons are reliable.

To use: extract archive into the repository root; no scripts.
Test: dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug
.NET build/test not executed in this environment.
