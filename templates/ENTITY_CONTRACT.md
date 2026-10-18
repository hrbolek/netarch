# Contract: add an entity to an existing domain

Apply the boundary requirements in [`DOMAIN_CONTRACT.md`](DOMAIN_CONTRACT.md)
to every generated or added type. Preserve existing persistence, DTO and service
generic interfaces and use the existing DI registration. Architecture annotations
are no longer used. Distinguish projections, associations, helper services and UI
view models by their actual contracts rather than assuming every `*Model` is an
entity.

All mandatory patterns are defined below and in `REFERENCE_CATALOG.md`; historical `User`, `Group`, `Membership` classes are optional provenance, **not required source dependencies**.

## Full vertical slice (unless expressly scoped narrower)

| Layer | Requirement | Reference |
|---|---|---|
| Models | Secured persistence model; response projection; create/update DTO; stable UUID; audit, relations | `Entity*Template` + corresponding contract |
| Services | Interface and secured service; validate invariants; `CreationScopeGuard` for Create and transactional `StageCreateAsync`/`SaveChangesAsync` | `Entity*Template` + corresponding contract |
| Resolvers | List, view, create, edit/update, delete; link/actions endpoints; Minimal API/Swagger; HTML/JSON | `Entity*Template` + corresponding contract |
| UI | `PageViewList`, `MediumCard`, `PageView`, `LargeCard`, `PageEdit`, editable form; context-aware create, EntityLinks | `Entity*Template` + corresponding contract |
| Relations | Related DTO labels/links and vector pages; efficient secured collection query | `Entity*Template` + corresponding contract |
| Integration | Wire DI and endpoint registration in domain module; connect schema | inspect selected domain |
| Verification | Build, tests for authorization, CUD, HTML, JSON, empty list | check against actual Host |

## Security invariant

For `CreateContained` the **container group** is the object on which create permission is checked. The newly created model's `rbacobject_id` is derived by the service. See the Create security invariant here and the included service-template contract; inspect compiled `CreationScopeGuard` public APIs when available. Do **not** directly copy the old `EntityModelTemplate` approach in which the client can supply an optional `rbacobject_id` or a Root default. Which creation method is correct for a non-contained entity must be explicitly designed against the security model.

## Special cases

- Group hierarchy: check parenting, cycle prevention and relevant authorization, using explicit hierarchy invariants and authorized parent selection.
- Membership: links a user and group with validity interval, **not** a role assignment; do not alter authorization roles when validity changes.
- Multiple visualizations: add extra typed components under existing page/endpoint mechanisms; see `VISUALIZATION_CONTRACT.md`.

## Inputs and relation editing

Each editable scalar/FK requires a Create/Update field map and the appropriate input. Follow [`INPUT_COMPONENTS_CONTRACT.md`](INPUT_COMPONENTS_CONTRACT.md). In particular, use existing Core `EntityLookupSelect` with the authorized related-entity resolver, `InitialId`/`InitialLabel` for Update, and server-side FK/RBAC validation. Do not turn a known GUID into an editable raw UUID text box.


## Endpoint metadata requirements (v140)

For resolver generation, first read [`ENDPOINT_CONTRACT.md`](ENDPOINT_CONTRACT.md) and the self-contained `EntityResolversTemplate/EntityNameEndpointExtensions.cs`. Use canonical routes, `WithName`, `WithDescription`, `WithTags`, `ApplyViewModel` and `ApplyJsonModel`; verify Swagger metadata. Core/User and Core/Group source code is **not required**.
