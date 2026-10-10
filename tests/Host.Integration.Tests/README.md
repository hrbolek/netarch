# Stage 3 – Host integration tests (first increment)

Run `dotnet test tests/Host.Integration.Tests/Host.Integration.Tests.csproj -c Debug`.

These tests bootstrap the *actual* Host and invoke HTTP endpoints with WebApplicationFactory. Each test has its own SQLite database and disables system-data import. No EF provider comparison or provider-specific SQL is attempted (Stage 99).

Coverage: `T11` startup, route registration and nonexistent entity/policy, `T13` proxy identity propagation and no-identity behavior. Additional Stage 3 work remains: full successful Request/Submission transition scenarios, actor-dependent authorization with seeded entities, authenticated negative writes, snapshots and HTML/JSON negotiation.

The Host exposes `public partial class Program` only to allow test-host discovery. **The current CoreDatabaseHostedService has SQLite-specific developer schema repairs; these are pre-existing production code and should eventually be replaced by provider-agnostic migrations (Stage 99).**

Build and runtime of these new tests must be verified in user's .NET environment. The previously confirmed baseline is 91/91.
