# Architecture rules and agent framework

## Authority and conflict handling

Apply sources in this order:

1. An architectural decision in [`decisions/`](decisions/) explicitly marked
   **Accepted**.
2. Verified architectural contracts and invariants.
3. Current source code and tests.
4. Established implementation conventions.
5. Agent recommendations.

Do not infer that a proposal or an unlabelled architecture document is an
accepted decision. If two sources conflict, report the conflict, cite both
sources, and ask for direction when it changes behavior or public contracts.
The stable rules, evidence locations, and verification commands are in
[`rules.yml`](rules.yml). The rules describe repository policy; AI reviews do
not enforce them. Deterministic tests and CI checks remain the enforcement path.
The Copilot adapter follows the
[VS Code custom-agent format](https://code.visualstudio.com/docs/agent-customization/custom-agents);
portable skills follow the
[Agent Skills format](https://code.visualstudio.com/docs/agent-customization/agent-skills).

## Optional use

Canonical role specifications are in [`../../agent-roles/`](../../agent-roles/).
Portable skill sources are in [`../../.agents/skills/`](../../.agents/skills/);
matching copies in [`../../.github/skills/`](../../.github/skills/) enable
Copilot's workspace skill discovery and are checked for exact parity.
Vendor adapters live separately, including Copilot under
[`../../.github/agents/`](../../.github/agents/). These are on-demand tools.
There is no required orchestrator, mandatory agent approval,
application runtime dependency, paid AI service, LLM key, or automatic AI
review in CI. The repository remains buildable and maintainable with standard
.NET, IDE, CLI and Git workflows.

## Runtime capability matrix

| Runtime | Thin adapter / entry point | Portable knowledge available | Runtime-specific extensions |
|---|---|---|---|
| Any assistant or no assistant | [`../../AGENTS.md`](../../AGENTS.md) | Architecture rules, canonical role specs, skills and deterministic validation | None required |
| GitHub Copilot in VS Code | [`../../.github/agents/`](../../.github/agents/), [`../../.github/copilot-instructions.md`](../../.github/copilot-instructions.md) | `AGENTS.md`, `agent-roles/`, synchronized `.github/skills/` | Profile tool lists, subagent availability and suggested handoffs |
| Claude Code | [`../../CLAUDE.md`](../../CLAUDE.md), [`../../.claude/agents/`](../../.claude/agents/) | `AGENTS.md`, `agent-roles/`, `.agents/skills/` (read by the adapter) | Claude tool names and subagent invocation |
| Cursor | [`../../.cursor/rules/zarchitecture.mdc`](../../.cursor/rules/zarchitecture.mdc) | `AGENTS.md`, `agent-roles/`, `.agents/skills/` | Cursor rule discovery and activation |
| Codex | [`../../AGENTS.md`](../../AGENTS.md) | Root instructions and standard `.agents/skills/` discovery | Codex-specific invocation/model behavior |

Portable content is the source of truth; Copilot skill files are validated
copies of the portable skill sources. Handoffs, tool permissions, hooks,
model selection and automatic skill loading vary by runtime; none is a
prerequisite for implementation or architecture validation.

## Generic infrastructure is the architecture

ZArchitecture deliberately reuses typed contracts across layers. Before
introducing entity-specific infrastructure, inspect the existing contracts and
implementations, including:

- `IEntityProjection<TDBModel,TDTO>` and `IEntityMapper<TDBModel,TDTO>` for
  typed DTO projection/mapping.
- `ISecuredEntityService<TDBModel>` and `SecuredEntityService<TDBModel>` for
  secured entity operations.
- `TreeSecuredService<TDBModel>` for materialized-path trees and path-aware
  writes.
- `BaseResolvers` and `ApplyViewModel` / `ApplyJsonModel` for common endpoint
  behavior.
- Shared Razor components such as `PageView`, `PageViewList`, `PageEdit`,
  `LargeCard`, `EntityLookupSelect`, `EntityLink`, `EntityPermissions`, and
  vector components.
- Module registration and existing EF model-contributor patterns.

Generic infrastructure is not an end in itself. Do not add a generic layer just
to remove a small amount of straightforward entity code. Shared Core changes
require an explicit impact analysis and broader regression validation than a
local domain change.

## Agent handoff model

The Orchestrator is a task router, not a mandatory gate. Use only the specialists
needed:

| Work | Suggested flow |
|---|---|
| Simple new entity | Entity Developer → Test & Quality Engineer → Architecture Guardian |
| Generic Core change | Generic Architecture Engineer → implementation → Test & Quality Engineer → Architecture Guardian |
| Security-sensitive entity/operation | Entity Developer → Security Architect → Test & Quality Engineer → Architecture Guardian |
| Complex refactor | Migration & Refactoring Engineer → relevant implementation specialist(s) → Test & Quality Engineer → Architecture Guardian |
| Documentation only | Documentation Architect; add Guardian only when rules change |

Only parallelize independent investigation or validation. Coordinate changes to
shared interfaces and generic constraints. Handoffs should name the task,
scope, evidence and requested outcome; do not assume delegated work is complete
until its result is read and validated. When subagents or explicit handoffs are
unavailable, follow the same sequence directly in one assistant or conventional
developer workflow.

## Measurable validation

- Run `python3 tools/validate-agent-framework.py` after changing profiles,
  skills, or the structured rules.
- For source changes, run the smallest relevant build/test command.
- For architecture changes or new entities, run
  `./tools/audit-architecture.sh`; it performs Release build/tests and direct
  plus transitive NuGet vulnerability checks. It does not run an AI review.
- For shared Core generic changes, include focused regression tests and
  validate downstream layers via the solution build/test.
- Report commands not run, remaining test gaps, warnings, and vulnerabilities
  explicitly. Do not claim a boundary is verified by a route-presence test
  alone.
