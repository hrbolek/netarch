---
name: generic-service-extension
description: Extend ZArchitecture secured or tree services while preserving RBAC filtering, audited writes, and transaction boundaries.
---

## When to use

Use when adding a service operation, query, related-entity projection, write
workflow, or tree behavior to a domain.

## Prerequisites

Read `ZA-AUTH-001` and `ZA-GENERIC-001` in `docs/architecture/rules.yml`;
inspect `ISecuredEntityService<TDBModel>`,
`src/Core/AuthorizationLayer/SecuredEntityService.cs`, and
`TreeSecuredService<TDBModel>` where hierarchy applies.

## Procedure

1. Inspect the closest Core or Events service, its contract, DI registration,
   model scope, and positive/negative behavior tests.
2. Compose ordinary reads from operation-filtered secured sources. Apply
   filters and typed DTO projections before materialization where possible.
3. Independently scope related entity data when required; a navigational DTO
   projection alone does not apply the related entity's RBAC policy.
4. Stage writes through `IAuthorizationLayer` and save inside its transaction.
   Preserve concurrency tokens and derive audit fields from trusted context.
5. For trees, use `TreeSecuredService<T>` path-aware create/move/delete helpers
   and test cycles, descendants, authorization, and path maintenance.
6. Justify every raw query or persistence write by a specific invariant or
   narrowly scoped command, and document its independent scope/actor binding.

## Architectural constraints

- Never expose an unrestricted query or privileged persistence path as an
  ordinary service/API operation.
- HTTP actor checks do not replace service-level RBAC; denial must not disclose
  protected existence or mutate state.
- Do not duplicate secured/tree service mechanics in a domain service.

## Validation

- Add tests for permitted and denied reads/writes, transaction and concurrency
  behavior, related data scope, and tree invariants where applicable.
- Run the domain test project and build its downstream consumers.
- For shared service contracts, include the full solution checks and
  `./tools/audit-architecture.sh`.

## Expected outputs

Report the service contract and implementation touched, operation/scope policy,
transaction and concurrency semantics, any justified privileged invariant,
tests/build results, and unresolved security risks.
