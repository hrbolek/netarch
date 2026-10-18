# ZArchitecture 77.12 — Separate development JsonataPlayground application

- Moved diagnostic UI and API into independent `src/Apps/JsonataPlayground` project.
- Host conditionally references and compiles the app when `EnableJsonataPlayground=true` (default for `Debug`, disabled by default in `Release`).
- `Program.cs` uses `#if JSONATA_PLAYGROUND`; no symbol, namespace or assembly dependency in default Release builds.
- Runtime mapping additionally requires `ASPNETCORE_ENVIRONMENT=Development`.
- Endpoints stay `GET /jsonata`, `POST /jsonata/execute`; diagnostics use application-controlled `DigitalProcesses:DataBackend` and `ForwardHeaders`.
- To run: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Host/Host.csproj -c Debug`.
- For production: `dotnet publish src/Host/Host.csproj -c Release`.
- Opt out even in Debug: `dotnet build src/Host/Host.csproj -c Debug -p:EnableJsonataPlayground=false`.
- Explicit opt in Release, for isolated troubleshooting only: `-p:EnableJsonataPlayground=true`, but still requires environment Development for route mapping.
- Sensitive header values are returned for diagnostics; use an isolated developer machine, never a publicly reachable instance.

Build was not run here (dotnet SDK unavailable).
