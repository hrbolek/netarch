---
name: architecture-discovery
description: Trace ZArchitecture contracts, project boundaries, and source-of-truth examples before implementing or reviewing a change.
---

## When to use

Use before changing a domain, endpoint, UI, shared Core contract, dependency
boundary, security path, migration, or architecture documentation. Skip it for
trivial edits whose ownership and behavior are already known.

## Prerequisites

- Start at `AGENTS.md` and read `docs/architecture/README.md` and
  `docs/architecture/rules.yml`.
- Select the relevant contract under `templates/`; for a domain use
  `templates/DOMAIN_CONTRACT.md` and `templates/REFERENCE_CATALOG.md`.
- Identify the requested behavior and its affected projects before editing.

## Procedure

1. Classify the change and map project references from the owning `.csproj`;
   folder and assembly names alone do not prove architectural responsibility.
2. Trace actual interfaces, generic constraints, base classes, DI registration,
   EF integration, and endpoint/UI calls that implement the behavior.
3. Compare the closest working examples: `src/Core/Group/`,
   `src/Core/User/`, and `src/Apps/Events/`; use the templates as normative
   contracts where they say so.
4. Find behavior tests, boundary tests, and decision records. Record conflicts
   among accepted decisions, contracts, implementation, and tests.
5. Before a shared Core change, list affected consumers and compatibility,
   authorization, and generic-constraint implications.

## Architectural constraints

- Apply the authority order in `docs/architecture/README.md`; recommendations
  are not accepted decisions.
- Preserve compile-time layer direction and use existing generic extension
  points before creating local or shared infrastructure.
- Never bypass secured service boundaries for ordinary reads or writes.
- Ask for direction if conflicting evidence changes runtime behavior or a
  public contract; otherwise report the conflict without silently resolving it.

## Validation

- Confirm every cited file, project reference, test, and decision exists.
- Run the smallest relevant deterministic test/build after implementation.
- For an entity or architecture change, run `./tools/audit-architecture.sh`.

## Expected outputs

Return a short evidence map: owning projects and layers, relevant contracts and
implementations, closest example, tests, rule IDs, and any unresolved conflict
or impact requiring approval.
