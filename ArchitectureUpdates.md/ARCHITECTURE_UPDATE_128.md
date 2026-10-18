# ZArchitecture 128 — Oprava CS0246 v Host/Program.cs

V `src/Apps/Host/Program.cs` se v `.ApplyViewModel<ApplicationDashboardPage, ApplicationDefinition>()` používá `ApplicationDefinition`, jehož namespace je `WebAppArchitecture.Base.UI.Routing`. Chyběl import tohoto namespace.

Přidáno `using WebAppArchitecture.Base.UI.Routing;` do podmíněného bloku `#if !ARCHITECTURE_API_ONLY`. API-only varianta tímto importem není dotčena. Jiné soubory zdrojového kódu se nemění.

## Ověření

Archiv ZIP je ověřen na integritu. V prostředí není dostupné .NET SDK, takže kompilaci nemohu potvrdit.

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug
```
