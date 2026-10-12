# ZArchitecture 107 — DigitalProcesses persistence boundary

## Implemented

- Moved all 18 DigitalProcesses EF entity source files from `src/Core/DBLayer` to `src/Apps/DigitalProcesses/DigitalProcesses.Models/DatabaseModels`.
- Updated their namespace to `WebAppArchitecture.DigitalProcesses.DBLayer` and retained Core base contracts through explicit imports.
- Updated source imports in DigitalProcesses application projects and DigitalProcesses integration tests.
- Moved DigitalProcesses-specific `IProcessRequestProvisioner`, `IProcessRequestWriteScope`, and `ProcessRequestProvisioner` from Core SecurityLayer to DigitalProcesses.Services/Security, preserving the transaction-scoped, allowlisted request provisioning capability.
- Kept `CoreDbContext` limited to Core tables and `DigitalProcessesDbContext : CoreDbContext` responsible for DP EF mappings and tables.
- No schema/table names or field types were deliberately changed; existing persistence mappings remain in the application module.

## Architectural boundary

Core DBLayer has no source declarations of DigitalProcesses entity types. DigitalProcesses.Models depends on Core DBLayer, not the other way around. The application host composes the derived EF context and shares it with Core via DI.

## Verification scope

Performed static checks for leftover model declarations and dependency direction, as well as ZIP integrity. The .NET SDK is not installed here, so compilation, test execution, and verification of resulting PostgreSQL migrations remain to be performed. Examine EF migration snapshots before applying migration changes to production data.
