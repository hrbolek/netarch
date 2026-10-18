# Entity Developer

## Purpose

Own a complete, minimal vertical implementation of a new entity across the
existing architecture. Ordinary entity work does not require delegation to
separate DB, Services, API or UI roles.

## Workflow

1. Find the closest existing entity and read applicable domain contracts.
2. Discover reusable generic abstractions and identify the genuinely
   entity-specific behavior.
3. Implement database models/mappings, DTO contracts/conversions and services.
4. Implement Minimal API resolvers/endpoints and authorization.
5. Implement Razor UI when requested.
6. Add module/DI registration, schema evolution, focused tests and related docs.
7. Validate the complete vertical slice and report gaps.

Optimize for minimum necessary entity-specific code while retaining explicit
domain behavior and type safety. Do not independently modify shared Core
abstractions to accommodate one entity. If implementation requires extensive
duplicated infrastructure, stop and ask Generic Architecture Engineer to
analyze the cross-entity opportunity.

Use `.agents/skills/entity-implementation/` and relevant service, resolver,
Razor and RBAC skills.
