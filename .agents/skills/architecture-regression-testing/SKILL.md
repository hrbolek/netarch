---
name: architecture-regression-testing
description: Select deterministic builds and tests that prove ZArchitecture behavior, security, compatibility, and layer boundaries.
---

## When to use

Use when selecting or running validation for source, security, schema,
architecture, generic Core, entity, or framework/configuration changes.

## Prerequisites

Identify changed projects, behavior, test projects, and available build
configuration. Read `docs/architecture/rules.yml` and repository test guidance;
never depend on an AI service, paid API, or LLM key.

## Procedure

1. Map each requirement to a specific test assertion, build target, or audit;
   prefer focused coverage before running broader suites.
2. Test behavior, not only route presence or successful compilation. Cover
   allow/deny paths, persistence, transactions/concurrency, representation,
   migration or schema upgrade as applicable.
3. For an ordinary local change, run the narrowest relevant test project and
   build its downstream consumer.
4. For shared generic Core, build `SignFakeArchitecture.sln` and run relevant
   downstream tests, including `tests/Core/Host.Integration.Tests`.
5. For architecture changes or new entities, run `./tools/audit-architecture.sh`.
   This is a deterministic build/test and NuGet security audit; it is not an AI
   review or an architecture classifier.
6. Capture exact command, exit status, pass/fail/skip counts, warnings,
   vulnerabilities, and any provider/schema paths not exercised.

## Architectural constraints

- Keep checks deterministic and runnable through ordinary .NET/CLI workflows.
- Do not disable security checks or claim that an unrun command succeeded.
- Do not expand to a full suite without a reason tied to coverage or failure.

## Validation

- Confirm the test result corresponds to the current revision and includes the
  intended test files/targets.
- Run `python3 tools/validate-agent-framework.py` for role, skill, or rules
  changes.

## Expected outputs

List commands with exact pass/fail/skip totals, warnings/audit findings,
requirements each check covers, and explicitly untested cases.
