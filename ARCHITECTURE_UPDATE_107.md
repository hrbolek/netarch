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

### Current entity-first layout

Entity sources now reside in `DigitalProcesses.Models/<Entity>/` with namespaces
`WebAppArchitecture.DigitalProcesses.Models.<Entity>`, alongside their DTOs.
`TemplateFieldBindingModel` has its own file and directory; there are still 19
entity types. Request provisioning now resides in `DigitalProcesses.Services/Request/`
and trusted submission reading in `DigitalProcesses.Services/Submission/`.
EF configuration remains in the Module project. Table, column, key, index and
foreign-key mappings are unchanged; offline SQL generation using the original
and reorganized namespaces produces an identical SHA-256 fingerprint, guarded
by `EntityOrganizationTests`. No migration was added or applied.

## Verification scope

Performed static checks for leftover model declarations and dependency direction, as well as ZIP integrity. The .NET SDK is not installed here, so compilation, test execution, and verification of resulting PostgreSQL migrations remain to be performed. Examine EF migration snapshots before applying migration changes to production data.
