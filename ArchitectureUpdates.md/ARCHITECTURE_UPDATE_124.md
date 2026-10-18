# ARCHITECTURE_UPDATE_124 — Compile-time API/UI host selection

## Implemented

- `src/Apps/Host/Program.cs` rewritten as a compact Minimal API composition root: module registration, shared database registration, build, middleware, routes, start/export/shutdown.
- Removed runtime `HOST_MODULE_MODE` parsing and the associated branching.
- Compile-time `ArchitectureApiOnly` MSBuild property controls `ARCHITECTURE_API_ONLY` constant; default build remains UI for compatibility.
- API variant does not register Razor components, root redirect or UI landing page; UI variant does.
- Preserved module routes, route prefixes, Swagger export and optional development Jsonata.

## Build

```sh
dotnet build src/Apps/Host/Host.csproj
dotnet build src/Apps/Host/Host.csproj -p:ArchitectureApiOnly=true
dotnet test SignFakeArchitecture.sln
```

## IMPORTANT: NOT YET FULL BINARY SEPARATION

This release does **not** fully remove UI assemblies from the API project's transitive dependency graph. `Core.Hosting` still references UI types; `DigitalProcesses.Module` still references `DigitalProcesses.UI`; `Base.csproj` is a Razor SDK project and includes Razor sources. The API build chooses behavior at compile time but still references Razor-containing projects. Claiming otherwise would be misleading.

Genuine separation requires extracting Base.UI from Base, creating UI-free API resolvers (or splitting presentation mapping into companion UI projects), and moving UI-specific module registration/mapping into optional UI assemblies. Those changes require compilation of both graphs and their integration tests. The .NET SDK is not available in this environment, so they were not attempted speculatively.

## Validation

Static inspection, archive ZIP integrity; **no .NET build or test** in this environment.
