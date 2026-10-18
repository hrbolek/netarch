# ZArchitecture 106 — dědičné EF Core DbContexty

- `CoreDbContext` in `Core/DBLayer` declares only Core DbSets and mappings.
- `DigitalProcessesDbContext : CoreDbContext` in `DigitalProcesses.Module` declares application DbSets and app-specific mapping.
- `DigitalProcessesModelBuilderExtensions` moved out of Core DBLayer into DigitalProcesses.Module, including document-template mappings.
- `CoreModule` registers a standalone CoreDbContext; `DigitalProcessesModule` replaces that descriptor and registers `DigitalProcessesDbContext` with a scoped CoreDbContext alias pointing to the SAME instance.
- Core stores, authorization, initializer, and IDBLayer continue to depend on CoreDbContext, not on DigitalProcesses.
- App-specific direct EF consumers depend on DigitalProcessesDbContext.
- PostgreSQL (Npgsql) remains the only runtime provider.

**Scope:** DB model POCO classes are still located in Core/DBLayer for compatibility with preexisting services and dependencies. This change isolates EF model assembly/configuration and context ownership, not the full domain-model assembly graph.

**Schema management:** Existing hosted services use `EnsureCreatedAsync`. It cannot evolve an existing schema. Do not treat this refactor as an EF migration; production migration ownership and migration scripts remain to be designed.

**Validation:** Static checks only; .NET SDK is unavailable in this environment.
