---
name: generic-abstraction-discovery
description: Evaluate existing ZArchitecture generic contracts and extension points before adding entity-specific or shared infrastructure.
---

## When to use

Use when implementation appears to need a new generic interface/base class,
shared service or resolver behavior, DTO mapping contract, open-generic DI
registration, EF integration hook, or reusable Razor component.

## Prerequisites

Read `docs/architecture/rules.yml` (especially `ZA-GENERIC-001` and
`ZA-CORE-001`) and identify the concrete requirement and at least one real
consumer.

## Procedure

1. Inspect `IEntityProjection<TDBModel,TDTO>` and `IEntityMapper<TDBModel,TDTO>`
   in `src/Core/Contracts/DTO/`, and
   `ISecuredEntityService<TDBModel>` in
   `src/Core/Contracts/Services/ISecuredEntityService.cs`.
2. Trace `SecuredEntityService<TDBModel>` and `TreeSecuredService<TDBModel>` in
   `src/Core/AuthorizationLayer/`, including protected query/write helpers.
3. Inspect `BaseResolvers` and `ViewModelEndpointExtensions` in
   `src/Core/Base/Resolvers/`, module/DI wiring, EF contributors, and
   `src/Core/Base/UI/Components/` as relevant.
4. Search at least two actual consumers across domains. Compare constraints,
   behavior, duplicated logic, extension points, and any competing abstraction.
5. State whether the current mechanism fully covers the behavior; if not,
   propose the smallest extension and identify all affected consumers.

## Architectural constraints

- Preserve generic constraints, static abstract members, typed projection and
  mapping, operation-specific authorization, and compile-time guarantees.
- Generalize only demonstrated cross-domain behavior. Do not add indirection to
  remove a few straightforward lines or support hypothetical flexibility.
- Shared contract changes require explicit compatibility/security impact
  analysis and downstream regression tests (`ZA-CORE-001`).

## Validation

- Search references/implementations of every changed contract and verify DI
  registration paths.
- Build the solution and run focused tests for representative consumers.
- Run `./tools/audit-architecture.sh` for shared architecture changes.

## Expected outputs

Provide the current extension point and code evidence, concrete consumers,
constraints and trade-offs, smallest justified change (or why no abstraction is
needed), compatibility/security effects, and targeted validation.
