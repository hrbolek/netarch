# ZArchitecture62 – SQLite DateTimeOffset ORDER BY compatibility

- ProcessDefinitionVersionResolvers.Lookup forms/workflows: SQL sorting by version descending, then id, keeping Take(25) and RBAC filtering server-side. Avoids unsupported SQLite DateTimeOffset ORDER BY.
- RequestResolvers.LookupProcessVersions: same change for published process versions.
- RequestService.GetDetailAsync: transition history is loaded for one request using the existing authorized query; sorting by changed_at happens after ToArray(), on the client, because SQLite cannot order by DateTimeOffset.

No database schema changes. This package has not been built with .NET SDK here; run `dotnet build src/Host/Host.csproj` locally.
