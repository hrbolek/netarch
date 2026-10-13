# Application template

Run from repository root:

```bash
dotnet new install ./templates/ApplicationTemplate
dotnet new architecture-app -n Projects -o src/Apps/Projects --rootNamespace WebAppArchitecture
dotnet sln SignFakeArchitecture.sln add src/Apps/Projects/Projects.Module.csproj
dotnet add src/Apps/Host/Host.csproj reference src/Apps/Projects/Projects.Module.csproj
```

In `Apps/Host/Program.cs`, add `host.Services.AddProjects()` before `AddHostDatabase` and `app.MapProjects()` after `Build()`.
No Core or HostDbContext modifications are needed. The module registers an `IAppModelContributor` automatically.

Two secured EF entities, their DTO projections, services and Minimal API CRUD read/update/delete endpoints are generated. **Create is intentionally left to the domain**, which must register RBAC objects and enforce invariants.


### IModule lifecycle (ZArchitecture 118)

`AppNameModule : IModule` contributes DI registrations, EF mappings, and API endpoints.
Register via `services.AddAppName()` before `host.Services.AddHostDatabase(...)`.
The host calls `app.ConfigureModules()` and `app.MapModules(prefixes, ModuleMode.Ui)` once.
For an API-only module, `MapUI` defaults to `MapAPI`. The existing Core and
DigitalProcesses modules still use Razor-enabled resolvers; complete UI-free
compilation is a separate migration.
