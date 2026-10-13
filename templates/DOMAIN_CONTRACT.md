# Contract: create a new domain

## Normative reference — standalone package

Read `PORTABLE_CORE_CONTRACT.md` and `REFERENCE_CATALOG.md` first. Domain architecture has **four independent projects**: `Domain.Models`, `Domain.Services`, `Domain.Resolvers`, `Domain.UI`, plus composition/module adapter as required. This is specified here, **not delegated to Core domain sources**. The existing `ApplicationTemplate` is legacy and requires completion. Check exact APIs using referenced binary assembly metadata/XML docs.

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


## Endpoint metadata requirements (v140)

For resolver generation, first read [`ENDPOINT_CONTRACT.md`](ENDPOINT_CONTRACT.md) and the self-contained `EntityResolversTemplate/Resolvers/EntityNameEndpointExtensions.cs`. Use canonical routes, `WithName`, `WithDescription`, `WithTags`, `ApplyViewModel` and `ApplyJsonModel`; verify Swagger metadata. Core/User and Core/Group source code is **not required**.
