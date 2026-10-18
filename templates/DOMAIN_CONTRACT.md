# Contract: create a new domain

## Normative reference — standalone package

Read `PORTABLE_CORE_CONTRACT.md` and `REFERENCE_CATALOG.md` first. Domain architecture has **four independent projects**: `Domain.Models`, `Domain.Services`, `Domain.Resolvers`, `Domain.UI`, plus composition/module adapter as required. This is specified here, **not delegated to Core domain sources**. The existing `ApplicationTemplate` is legacy and requires completion. Check exact APIs using referenced binary assembly metadata/XML docs.

## Source layout

Directory names express ownership; project names identify assemblies. For a
small single-entity project, place its C# sources beside `Entity.Models.csproj`,
`Entity.Services.csproj` or `Entity.Resolvers.csproj`, not inside an additional
`Models`, `Services` or `Resolvers` directory. Keep UI `Pages` and `Components`
subdirectories, and entity-specific folders in larger multi-entity projects.
An independently exposed entity owns its contracts/services/resolvers; do not
hide another entity's types in an unrelated owner's project just because the
entities are related. Do not create empty projects or invent missing UI.

In a source checkout, the shared contract project is
`src/Core/Contracts/Core.Contracts.csproj`, with assembly name `Core.Contracts`.
Its dependency-free public contracts retain their existing namespaces.
The reference Core DBLayer groups each entity's model/store types under
`Entities/{Entity}` and RBAC projection types under `Authorization`, without
splitting its DbContext or changing persistence namespaces. New application
domains still own their model/mappings through the existing contributor pattern.

## Mandatory deliverables

1. Domain-specific `.Models` project: database models and DTO requests/responses/projections; model contributor / EF mappings connected through existing module/database composition.
2. `.Services`: interface, secured business implementation, validation, transactions, audited changes, batch secured queries for list views.
3. `.Resolvers`: thin Minimal API methods and `Map...Endpoints` extension, canonical URI under module prefix, `ApplyViewModel` and `ApplyJsonModel`, Swagger descriptions.
4. `.UI`: own components (`MediumCard`, `LargeCard`, editable content and relation vectors); pages for list/detail/create/edit; navigation and links.
5. Module registration through existing `IModule`/Host mechanisms; no direct modifications to Core's model or to unrelated modules.
6. DB schema initialization/migration for *existing* deployments as well as empty databases; seed/import only for explicitly specified reference data.
7. Tests: generate/build, basic GET JSON/HTML, create/update/delete with positive/negative RBAC, empty list, navigation and database upgrade.

`ApplicationTemplate` currently scaffolds a one-project example without all the above. This is **NOT** a conformant new-domain implementation by itself. Use this four-layer contract and the included `Entity*Template` examples; no cloning of Core source projects is necessary or allowed as a prerequisite.

## Decision points to resolve in the spec

Identify ownership/security scope for Create; nullable and required relationships; delete behavior; hierarchical or interval constraints; API representations; specialized visualizations; required standard data and imports. Do not invent an unrestricted security scope, silent cascade behavior, or empty UI by omission.

## Architecture boundaries and validation

Architecture annotations and the attribute-driven auditor have been removed.
Do not add attribute-library references to generated projects. Preserve existing
database model, request/response DTO and service generic interfaces, and wire
their existing implementations through DI. Determine responsibilities from
actual interfaces, base classes and generic arguments; suffixes and directories
are conventions, not proof of a role. Do not invent paired DTOs or services
solely to satisfy naming patterns, and do not rename existing types.

After an entity or architecture change run `./tools/audit-architecture.sh`.
This compatibility entry point runs the application build, tests and direct/
transitive NuGet vulnerability audit; it no longer produces an architecture
classification report. Validate boundaries with the existing contracts and
integration tests, and report any unverified boundaries explicitly.


## Endpoint metadata requirements (v140)

For resolver generation, first read [`ENDPOINT_CONTRACT.md`](ENDPOINT_CONTRACT.md) and the self-contained `EntityResolversTemplate/EntityNameEndpointExtensions.cs`. Use canonical routes, `WithName`, `WithDescription`, `WithTags`, `ApplyViewModel` and `ApplyJsonModel`; verify Swagger metadata. Core/User and Core/Group source code is **not required**.
