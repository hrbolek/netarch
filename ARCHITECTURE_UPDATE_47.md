# Architecture update 47 — modular hosting

This update extracts reusable host/module composition from the executable Host into `Core.Hosting`.

## New hosting layer

`src/Core/Hosting/Core.Hosting.csproj` provides:

- `ArchitectureHost`
- `IArchitectureModule`
- generic module registration/mapping helpers
- `CoreModule`
- Core database startup/shutdown lifecycle
- Core host/root/diagnostic resolvers
- Swagger/host infrastructure

The module contract is deliberately small:

```csharp
public interface IArchitectureModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    void MapEndpoints(IEndpointRouteBuilder endpoints);
    void ConfigureNavigation(IApplicationNavigationRegistry navigation);
}
```

## Core module

`CoreModule` now owns the registrations that previously lived in `Host/Program.cs`:

- EF/SQLite and DBLayer
- security and EffectiveAccess projection
- User/Group/Membership/Role/RoleType services/stores
- actor/security services
- core endpoint mapping
- `/`, `/architecture`, `/info`, `/debug`
- Architecture navigation
- Core entity route registration

`CoreDatabaseHostedService` performs the existing lifecycle:

- EnsureCreated
- restore dump if present
- otherwise optional SystemData import
- security projection rebuild
- graceful shutdown dump

A restored dump still never gets mixed with external SystemData import.

## Digital Processes module

A new composition project exists at:

`src/Apps/DigitalProcesses/DigitalProcesses.Module`

It is the only Digital Processes project that knows all four application layers plus `Core.Hosting`.
It owns:

- Digital Processes DI registrations
- renderer registrations
- Digital Processes entity route registration
- Digital Processes navigation contribution
- `/digital-processes` application root
- all Digital Processes resolver mapping

This project is intended as the precedent for a future `Events.Module`.

## Thin executable Host

The executable host now contains essentially only the composition root:

```csharp
var host = ArchitectureHost.Create(args);

host.Services
    .AddCore()
    .AddDigitalProcesses();

var app = host.Build();

app
    .MapCore()
    .MapDigitalProcesses();

await app.RunAsync("http://0.0.0.0:8080");
```

The Host project directly references only:

- `Core.Hosting`
- `DigitalProcesses.Module`

## Dependency direction

```text
Core.Hosting
    ↑
DigitalProcesses.Module
    ↑
Host
```

`Core.Hosting` does not reference Digital Processes. Static project-reference analysis found no cycles.

## Deliberately deferred

This update modularizes host composition, DI, routing, navigation and lifecycle. It does **not** yet physically move the Digital Processes persistence models/configuration out of `Core/DBLayer`.

That remaining persistence coupling should be addressed separately when Core is prepared for publication as a standalone library/NuGet package, ideally by introducing modular EF model contributions. The new Host/module API is designed so that this later change does not require changing the executable `Program.cs` composition pattern.
