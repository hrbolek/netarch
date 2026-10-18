---
name: entity-implementation
description: Implement a minimal, secure ZArchitecture entity as a complete vertical slice using existing generic infrastructure.
---

## When to use

Use when adding an independently owned entity or a substantial entity-specific
capability that spans persistence, DTOs, services, API, UI, DI, or tests.

## Prerequisites

- Read `AGENTS.md`, `docs/architecture/rules.yml`,
  `templates/DOMAIN_CONTRACT.md`, `templates/ENTITY_CONTRACT.md`, and
  `templates/REFERENCE_CATALOG.md`.
- Resolve ownership, trusted create scope, required relationships, hierarchy,
  delete behavior, representations, and schema-upgrade requirements. Do not
  invent these domain decisions.
- Use `architecture-discovery` and `generic-abstraction-discovery` first.

## Procedure

1. Compare the actual domain implementation in `src/Core/Group/` or
   `src/Core/User/` and the cross-layer example `src/Apps/Events/README.md`;
   identify the closest match and reusable contracts.
2. Implement only required model/EF mapping, DTO projection or mapper, secured
   service, resolver endpoints, UI, module registration, schema evolution, and
   domain-specific behavior.
3. Route ordinary reads and writes through `SecuredEntityService<T>` or
   `TreeSecuredService<T>`, their service contracts, and the authorization
   transaction boundary.
4. Keep endpoints thin, HTML in Razor UI, and entity-specific extensions small.
   Do not change shared Core solely to accommodate this entity.
5. Add focused behavior and boundary tests for allowed/denied access,
   persistence, relationships, paging/representations, and upgrades as relevant.

## Architectural constraints

- Follow `ZA-LAYER-001`, `ZA-ENTITY-001`, `ZA-AUTH-001`, `ZA-API-001`, and
  `ZA-UI-001`; classify responsibilities from actual contracts, not names.
- Derive audit actor/timestamps from trusted server context. Preserve
  concurrency tokens and do not expose DB models as resolver responses.
- If substantial generic duplication appears necessary, stop and request
  generic architecture analysis before copying shared infrastructure.

## Validation

- Build affected projects; run focused positive and negative integration tests.
- Verify DI/module wiring, canonical routes and representations, and existing
  deployment schema behavior.
- Run `./tools/audit-architecture.sh` for a new entity or architecture change.

## Expected outputs

Summarize the vertical slice and reused extension points, decisions/assumptions,
changed projects, security and schema behavior, exact validation results, and
any explicitly unverified cases.
