---
name: generic-resolver-extension
description: Add ZArchitecture Minimal API endpoints using shared resolver helpers and fixed, purpose-appropriate authorization operations.
---

## When to use

Use when adding or changing a Minimal API route, list/vector query, endpoint
representation, or OpenAPI contract.

## Prerequisites

Read `templates/ENDPOINT_CONTRACT.md`,
`templates/EntityResolversTemplate/EntityNameEndpointExtensions.cs`,
`ZA-API-001`, and `ZA-AUTH-001`. Inspect
`src/Core/Base/Resolvers/BaseResolvers.cs` and
`ViewModelEndpointExtensions.cs`.

## Procedure

1. Trace the existing route through trusted actor resolution, service contract,
   service implementation, response type, and module endpoint registration.
2. Reuse `BaseResolvers` and `ApplyViewModel`/`ApplyJsonModel` when their
   constraints and response behavior fit.
3. Fix the RBAC operation according to purpose: ordinary entity listing uses
   `Read`; create-target pickers use `Create`. Never accept an operation from a
   query parameter.
4. Keep domain logic, authorization and persistence in services; the resolver
   shapes HTTP results only. Keep HTML rendering in Razor.
5. Define canonical path, stable `WithName`, meaningful business
   `WithDescription`, tags, representations and response/error metadata.
6. Test the route behavior, paging/filtering, denied/anonymous requests and
   JSON/HTML negotiation where exposed.

## Architectural constraints

- Never inject `DbContext`, raw persistence or RBAC policy implementations into
  ordinary endpoint handlers.
- Do not return DB entities, including nested in generic response shapes.
- Preserve route and representation compatibility; don't register
  nonfunctional placeholder endpoints.

## Validation

- Build resolver and downstream projects; inspect generated OpenAPI operation
  IDs, tags, descriptions, schemas and media types.
- Run focused endpoint tests with both permitted and denied behavior.
- Run broader solution checks for shared resolver contract changes.

## Expected outputs

List route/operation behavior, service and DTO contracts used, fixed
authorization purpose, representation/OpenAPI changes, exact test/build
results, and any unverified response or schema behavior.
