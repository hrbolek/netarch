# Agent instructions for creating ZArchitecture domains and entities

## Mandatory discovery

1. Determine whether the task is **new domain**, **new entity**, **new operation**, or **new visualization**; multiple contracts may apply.
2. Read `PORTABLE_CORE_CONTRACT.md`, `REFERENCE_CATALOG.md` and the task-specific contracts in `/templates`. **Never require** `src/Core/User`, `src/Core/Group`, `src/Core/Membership` or `src/Core/Base` source trees; verify the public API of the installed DLL/NuGet instead.
3. Select the closest **included** recipe from `REFERENCE_CATALOG.md`: actor-scoped Create, hierarchical relations, or link entities with validity intervals; these patterns originated in Core domains but must be completely described by `/templates/`.
4. List the required changes across Models, Services, Resolvers, UI, module registration, schema, security, tests and documentation *before* coding.

## Binary-only delivery constraint

No generation, validation or documentation step may depend on viewing Core domain source code. Compiled public contracts and the included examples are sufficient; if missing, explicitly flag the template/SDK gap.

## Hard constraints

- One domain follows the reference **four independently named projects**: `Domain.Models`, `Domain.Services`, `Domain.Resolvers`, `Domain.UI` (and a module/composition adapter as needed). Each layer remains unidirectional, with no service depending on UI or HTTP.
- Reuse `SecuredBaseService`, `ISecurityLayer`, `CreationScopeGuard`, secured query helpers, transactional stage/save APIs and `BaseResolvers` when applicable. Creation's scope is a permitted container; **never trust an arbitrary client-supplied rbacobject_id** as authority.
- Use `Map...Endpoints`, canonical `/{prefix}/{ResponseType}/...`, `ApplyViewModel` / `ApplyJsonModel`, and explicit OpenAPI descriptions. HTTP handlers call services, not `DbContext`.
- Render with `PageViewList`, `PageView`, `PageEdit`, `MediumCard`, `LargeCard`, `EntityLink`/`LazyEntityLink`, `EntityPermissions`, `EntityLinks`, `ViewVector`, and existing Core primitives when they fit. For Create/Update fields, follow `INPUT_COMPONENTS_CONTRACT.md`: use the real `EntityLookupSelect` for foreign keys and never invent Core scalar controls. Do not introduce independent HTML/string-rendering or page systems.
- `Membership` is NOT a role grant. Membership validity must not implicitly change role validity, authorizations, or EffectiveAccess.
- Never modify Core or unrelated domains to solve a local domain problem without explicit authorization.
- Seed/import must be idempotent and preserve UUIDs and existing data. PostgreSQL `timestamptz` values must be UTC.

## Completion gate

- Source layout matches `DOMAIN_CONTRACT.md`; entity behavior matches `ENTITY_CONTRACT.md`; visualizations match `VISUALIZATION_CONTRACT.md`.
- Restore/build and test are required; test CRUD, RBAC denial, HTML and JSON views, navigation, relational links, existing database/schema changes, empty states, and repeatable imports.
- Clearly report unperformed checks. ZIP integrity != compiled or operationally tested software.
- `ApplicationTemplate` is currently **legacy**; do not automatically ship it as a complete domain, and do not claim its generated output meets the contract.


## Endpoint metadata requirements (v140)

For resolver generation, first read [`ENDPOINT_CONTRACT.md`](ENDPOINT_CONTRACT.md) and the self-contained `EntityResolversTemplate/Resolvers/EntityNameEndpointExtensions.cs`. Use canonical routes, `WithName`, `WithDescription`, `WithTags`, `ApplyViewModel` and `ApplyJsonModel`; verify Swagger metadata. Core/User and Core/Group source code is **not required**.
