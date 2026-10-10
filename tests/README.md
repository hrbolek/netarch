# Testing layout – ZArchitecture 78

- `Core.SecurityLayer.Tests/`: Stages 1–2 RBAC regressions (91 passed in user environment).
- `Core/Host.Integration.Tests/`: Stage 3 HTTP host, identity propagation, OpenAPI infrastructure.
- `Apps/DigitalProcesses.Integration.Tests/`: Stage 3 app-specific Request, Submission, policy and routing HTTP checks.
- `Shared/IsolatedHost.cs`: WebApplicationFactory fixture reused by both integration projects.

Run:

```sh
dotnet test tests/Core.SecurityLayer.Tests/Core.SecurityLayer.Tests.csproj -c Debug
dotnet test tests/Core/Host.Integration.Tests/Host.Integration.Tests.csproj -c Debug
dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug
```

Important: the new split and added checks have **not been compiled or run** here. Latest confirmed result: 91 Core SecurityLayer and 6 unsplit Host HTTP tests in the previous version. This increment includes a read-only `IEntityWithTree` contract, implemented by GroupModel, without EF schema or path mutation logic changes. A reusable EF-mapped record base class should be considered separately because it would change persistence and mapping behavior. No PostgreSQL/SQL Server verification before Stage 99.

Full request start/transition/snapshot authorization E2E needs controlled, authorized test seeding and remains separate from these HTTP smoke/security boundary checks. Do not count route-presence tests as lifecycle coverage.

Stage 3 extension 02 adds `Apps/DigitalProcesses.Integration.Tests/DigitalProcessesAuthorizationHttpTests.cs` (six additional HTTP cases). Positive state-transition and hidden-data preservation scenarios remain outstanding; see the app suite README. Cross-provider runs remain reserved for Stage 99.

### Stage 3: zbývající akceptační scénáře

Doplněny HTTP kontrakty v Apps/DigitalProcesses.Integration.Tests. Pozitivní procesní end-to-end scénáře se nesmějí označit za úspěšné bez přímého testu startu a transition nad publikovaným procesem v RBAC fixture. Integrace PostgreSQL a MSSQL se odkládají výhradně do Etapy 99.

### Stage 4 – T14 query scalability (initial increment)

`Core.SecurityLayer.Tests/Stage4QueryScalabilityTests.cs` contains three
provider-neutral regression tests: composed SQL queries with caller filters,
80 secured objects with Deny and paging, and nonduplication when two roles
grant the same operation. These are correctness/scalability invariants, **not**
throughput or memory benchmarks; no hardcoded SQLite SQL or time thresholds.
The positive DigitalProcesses Start → Transition → archive → Deny end-to-end
scenario remains outstanding from Stage 3; it is **not** considered tested.
Provider compatibility (PostgreSQL/MSSQL) is reserved for Stage 99.

Run: `dotnet test tests/Core.SecurityLayer.Tests/Core.SecurityLayer.Tests.csproj -c Debug`


## Stage 4 / T14 — large permissions & provider-neutral instrumentation

`Stage4LargeAccessTests.cs` checks one EF database command per secure list/count,
model-level `EffectiveAccess`/`RoleTypeOperation` indexes, and an opt-in
**1500-user stress fixture** with 1500 direct roles, inherited scopes and
materialized access rows. Membership is intentionally not a source of rights.

Normal: `dotnet test tests/Core.SecurityLayer.Tests/Core.SecurityLayer.Tests.csproj -c Debug`

Stress: `STAGE4_STRESS=1 dotnet test tests/Core.SecurityLayer.Tests/Core.SecurityLayer.Tests.csproj -c Release --filter FullyQualifiedName~T14_LargePopulation --logger 'console;verbosity=normal'`

The stress case measures full rebuild, single-user secure read, and incremental
resynchronization separately; timings are diagnostic, not pass/fail limits.
Do not interpret SQLite timings as PostgreSQL/SQL Server performance. Cross-provider
validation is deferred to Stage 99. This scenario does not import the production
1500-user JSON seed; it creates controlled synthetic data so that counts are known.

### Stage 4 complete – incremental impact regressions

`Core.SecurityLayer.Tests/Stage4IncrementalImpactTests.cs` adds five T14 tests:
adding and revoking a role must preserve unrelated users' projections;
repeating targeted synchronization must preserve projection contents;
a Deny update is limited to the affected branch; and a targeted role
update in the presence of 120 independent roles must use a bounded number
of EF Core commands (not a full rebuild). The 20-command bound is a generous
**query-count** regression guard, not a timing performance benchmark.

Stage 4 scope consists of query composition, non-N+1 list/count,
indexes, synthetic 1,500-user opt-in stress, and incremental impact.
Do not run a global rebuild on every production mutation. Timings are
diagnostic only. PostgreSQL/MSSQL compatibility stays in Stage 99.
The positive DigitalProcesses Start → Transition → archive → Deny
acceptance scenario is still outstanding in Stage 3; Stage 4 completion
does not imply completion of that separate scenario.
