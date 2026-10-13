# Architecture update 117 — Apps-first module templates

- Adds `src/Apps/AppComposition/IAppModelContributor` to express EF model contributions without Core changes.
- `HostDbContext` applies registered contributors from DI in module registration order; no hand-written `typeof(...).Assembly` in `OnModelCreating`.
- Adds `architecture-app` template with two related secured DB entities, projection/mapper DTOs, services, minimal API resolvers, EF mapping and registration via `AddAppName` / `MapAppName`.
- Repairs old entity templates for v114 generic DTO contracts and paths beneath Apps.
- Core remains unchanged; DigitalProcesses mapping remains inherited as transitional compatibility pending removal of the chain.
- No dotnet SDK available here: builds and tests must be run locally.

**Build:** `dotnet test SignFakeArchitecture.sln`. Create stays domain-specific and is intentionally not generated as generic bypass of RBAC.
