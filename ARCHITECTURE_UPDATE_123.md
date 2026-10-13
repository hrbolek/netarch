# ARCHITECTURE_UPDATE_123

## Changes
- `HostDbContext : CoreDbContext`, no Host → DigitalProcesses DbContext inheritance.
- `DigitalProcessesModule.ConfigureDatabase` contributes EF mappings to Host composition.
- DigitalProcesses bootstrap, template management, and RTF store use shared `CoreDbContext.Set<T>()`.
- Host database DI binds one scoped HostDbContext to CoreDbContext, without any app-specific DbContext alias.
- Razor service registration is explicit in Host and skipped for `HOST_MODULE_MODE=Api`; default remains `Ui`.
- Deleted unused obsolete `IArchitectureModule` compatibility interface.
- Adapted Host integration test to verify model composition rather than inheritance alias; updated two DigitalProcesses integration test fixtures to resolve CoreDbContext.Set<T> instead of the removed module-specific alias.

## Compatibility notes
- Existing DigitalProcessesDbContext remains available as a standalone context for existing isolated tests, but is not injected in composed Host.
- API-only Host does not register Razor services; physical project/assembly Razor dependencies and HTML-specific endpoint mappings are **not yet removed**. This is not a complete binary API/UI separation.
- Other Core/UI contracts and HTTP paths are unchanged.

## Verification
- Static checks and ZIP integrity verified. No .NET SDK in execution environment, so compilation/test results **not verified**.
- Run `dotnet test SignFakeArchitecture.sln` and test `HOST_MODULE_MODE=Api` explicitly.
