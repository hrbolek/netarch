---
name: architecture-compliance
description: Review ZArchitecture changes against stable rule IDs, verified evidence, and the approved authority hierarchy.
---

## When to use

Use for an independent architecture review, boundary audit, or review of a
change touching contracts, dependencies, services, resolvers, UI, authorization,
or architecture documentation.

## Prerequisites

Read `docs/architecture/README.md`, `docs/architecture/rules.yml`, accepted
records under `docs/architecture/decisions/`, and the target diff. Select only
relevant rules and actual source/tests.

## Procedure

1. Apply authority order: accepted decision, verified contract/invariant,
   current source/test, convention, recommendation.
2. Trace compile-time project references and actual generic contracts,
   service/resolver paths, DI, authorization, transactions, and UI ownership.
3. Verify each suspected issue against code and behavior tests; inspect what
   architecture tests do and do not cover.
4. Report each verified violation with stable rule ID, file/line evidence,
   severity, rationale, and concrete correction.
5. Separate unverified concerns and label all nonviolating suggestions
   **Recommendation**. Cite conflicting evidence without redefining policy.

## Architectural constraints

- This skill is for read-only review. Do not edit implementation files.
- Do not invent rules, infer responsibility from suffix/folder, or treat a
  successful build as proof of architecture compliance.
- Architecture rules are repository policy; agent conclusions do not replace
  deterministic CI or branch protection.

## Validation

- Confirm all cited evidence exists and line references correspond to the
  reviewed revision.
- Consult `ArchitectureDependencyTests` and `./tools/audit-architecture.sh`
  only as far as their actual coverage/results support conclusions.

## Expected outputs

Provide verified findings first with rule ID, severity, evidence, rationale and
correction; then recommendations, conflicts, scope inspected, and checks
actually executed. State clearly if no violations were verified.
