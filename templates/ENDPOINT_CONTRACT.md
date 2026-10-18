# Endpoint and OpenAPI contract (normative)

## Source of truth
This document and `EntityResolversTemplate/EntityNameEndpointExtensions.cs` are the **self-contained normative example**, usable with Core distributed as DLL/NuGet. Historical User and Group sources are optional provenance, not required input. Verify Core extension signatures in the installed public assemblies.

## Canonical route group

`endpoints.MapGroup(prefix + "/EntityNameResponse").WithTags("EntityName")`. The module owns `prefix` (e.g. `/projects`); do **not** prepend it in the entity mapping again. Use canonical names rather than ad-hoc `/entity-name/`.

## Metadata for EVERY mapped business endpoint

1. `.WithName("EntityNameOperation")`: unique, stable OpenAPI operation ID; include entity and action. Redirect aliases may be unnamed.
2. `.WithDescription("...business semantics...")`: describe input and output, filters/paging, RBAC scope, potential errors and side effects; no generic filler text.
3. `.WithTags("EntityName")` on the route group.
4. `.ApplyViewModel<TPage,TResponse>()` for UI-first GETs and form responses; `.ApplyJsonModel<TResponse>()` for API-first operations where the Core public extension supports it. Actual extension name is **ApplyJsonModel**, not `ApplyJSON`.
5. For endpoints with no representation extension (e.g. DELETE or redirects), explicitly document HTTP result types using `.Produces(...)` / `.ProducesProblem(...)` where suitable and supported; do not falsely claim all failures return ProblemDetails.
6. The representation helpers handle response content negotiation and metadata; avoid two overlapping `Produces(200)` calls that can hide one media type in Swagger. Core provides `ProducesEntity<T>` when necessary.

## Expected operation matrix

| Method | Path | Representation | Notes |
|---|---|---|---|
| GET | `/` | Redirect | `/list` |
| GET | `/{id}` | Redirect | `/view/{id}` |
| GET | `/list` | `ApplyViewModel<ListPage, Response[]>` | Filter/paging and RBAC |
| GET | `/view/{id}` | `ApplyViewModel<ViewPage, Response>` | Read |
| GET | `/edit/{id}` | `ApplyViewModel<EditPage, Response>` | Update UI |
| GET | `/create` | `ApplyViewModel<CreatePage, CreatePageModel>` | Creation scope / context |
| POST | `/create-contained` | `ApplyJsonModel<Response>` | Domain-owned service + RBAC |
| POST | `/create-form` | `ApplyViewModel<ViewPage, Response>` | HTML form submission |
| GET | `/link/{id}` | `ApplyViewModel<EntityLinkFragment,EntityLinkResponse>` | Link fragment |
| GET | `/actions/{id}` | `ApplyViewModel<EntityActionMenu,EntityActionMenuModel>` | Effective operations |
| POST | `/vector` | `ApplyJsonModel<Response[]>` | Structured filtering |
| PUT | `/update` | `ApplyJsonModel<Response>` | JSON-first |
| POST | `/update/{id}` | `ApplyViewModel<EditPage,Response>` | Form submission |
| DELETE | `/delete/{id}` | documented result | Authorized deletion |

The matrix describes the **target** entity contract. Only register endpoints for which the service, DTO, authorization and UI code truly exist. Do not add nonfunctional placeholders. Model a context-free Create only if the entity's authorization design supports it.

## Resolver constraints

Resolver code MUST NOT directly query or mutate `DbContext`, construct HTML strings, or implement a separate RBAC policy. Delegate business logic, authorization and transactions to the service layer. For relation/vector endpoints, reuse standard filter/paging and authorization infrastructure.

HTTP resolver classes no longer use architecture annotations.
Public endpoint return types must be response/transport contracts, never
database entities (including entities nested in generic return types). Do not
change established resolver names just to satisfy a suffix check.

## Verification (required)

- Build generated layers against the **actual installed Core binary** and check namespace/generic signatures.
- Inspect Swagger/OpenAPI for stable operationIds (`WithName`), tags, descriptions, schemas, status codes and both JSON/HTML representations as intended.
- Exercise `Accept: application/json` and `Accept: text/html` where representation negotiation is exposed.
- Test create/update/delete with both authorized and unauthorized actors; test form submissions and link fragments.
- If build or runtime checks cannot be run, explicitly report these as unverified.
