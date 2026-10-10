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
