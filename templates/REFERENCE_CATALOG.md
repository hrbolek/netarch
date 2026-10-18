# Reference catalog — work offline against binary Core

This catalog replaces all mandatory instructions to inspect `src/Core/User`, `src/Core/Group` or `src/Core/Membership` sources. The **actual recipe** lives in the named documents/templates below; historical domain names identify only the pattern's origin.

| User request | Normative instructions | Included implementation starting point | Public binary contract to inspect |
|---|---|---|---|
| New domain | `DOMAIN_CONTRACT.md` | `ApplicationTemplate/` (legacy; extend to four projects) | Module/composition DI, EF contributors, endpoint maps |
| New entity | `ENTITY_CONTRACT.md` | `EntityModelTemplate`, `EntityServiceTemplate`, `EntityResolversTemplate`, `EntityUITemplate` | Secured service/query/transaction contracts |
| List/detail/edit | `UI_COMPOSITION_CONTRACT.md` | `EntityUITemplate/Pages/*`, `EntityUITemplate/Components/*` | Generic pages, cards, link components |
| Create/contextual FK | `INPUT_COMPONENTS_CONTRACT.md` | FK example in that contract, UI editable skeleton | `EntityLookupSelect`, validation and Create security API |
| Hierarchy | `ENTITY_CONTRACT.md` (special cases) | Extend secured entity scaffolds | Query/filter and cycle checks |
| Link with validity | `ENTITY_CONTRACT.md` | Extend model + secured service | Membership-type linkage does not alter Role grants |
| Specialized visualization | `VISUALIZATION_CONTRACT.md` | Domain view component on standard page | Read DTO projection + authorized resolver |

## Required output for every generated entity

1. `Domain.Models`: EF model with UUID, audit/RBAC, request/response DTO and schema mapping.
2. `Domain.Services`: secured CRUD, Create authorized against server-resolved scope, business invariants, transactional persistence.
3. `Domain.Resolvers`: canonical typed Minimal API, consistent REST and HTML/JSON endpoints, OpenAPI and link/action endpoints where supported.
4. `Domain.UI`: standard list/detail/edit composition + Create; domain `MediumCard` and `LargeCard`, relator `EntityLink` and FK `EntityLookupSelect`.
5. Integration with module/DI, persistent migrations and registration; tests for RBAC, links, blank state, create/update and binary-only compatibility.

These are **required deliverables**, not evidence that the current `dotnet new` skeletons generate a complete vertical slice. Current limitations are recorded in `TEMPLATE_STATUS.md`.


## Endpoint metadata requirements (v140)

For resolver generation, first read [`ENDPOINT_CONTRACT.md`](ENDPOINT_CONTRACT.md) and the self-contained `EntityResolversTemplate/EntityNameEndpointExtensions.cs`. Use canonical routes, `WithName`, `WithDescription`, `WithTags`, `ApplyViewModel` and `ApplyJsonModel`; verify Swagger metadata. Core/User and Core/Group source code is **not required**.
