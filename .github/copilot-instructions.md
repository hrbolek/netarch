# Optional ZArchitecture specialist selection

Use these routing hints only when a task benefits from distinct architectural
expertise. They do not require custom agents, approvals, orchestration, or AI
review; ordinary Copilot and developer workflows remain fully supported. Honor
the user's explicit specialist choice.

## Select the smallest useful specialist

- A complete new entity or vertical slice: `entity-developer`.
- Shared Core contracts, generic constraints/base classes, DI/open generics,
  EF integration, generic resolvers, or reusable components:
  `generic-architecture-engineer` first; coordinate dependent implementation
  and checks in sequence.
- RBAC, trusted actors, visibility, transactions, or sensitive data:
  `security-architect`.
- Service and Minimal API endpoint changes without shared generic design:
  `api-services-engineer`.
- Razor presentation/component work: `razor-ui-engineer`.
- Behavior tests, builds, architecture checks, or security audit:
  `test-quality-engineer`.
- Behavior-preserving migrations/refactors: `migration-refactoring-engineer`.
- Evidence-backed docs/contracts: `documentation-architect`.
- Independent, read-only architecture review: `architecture-guardian`.

Use `zarchitecture-orchestrator` only when the task spans specialists, needs
sequencing, or the user requests orchestration. The orchestrator must pick
specialists by task and risk, not invoke every available agent. Simple entity
work remains with Entity Developer end to end.

## Delegation and review

If the selected runtime supports subagents, the orchestrator may invoke only
the needed agent references defined in its profile. Parallelize independent
investigation or validation, not edits to dependent shared contracts. Verify
delegated results before claiming completion. After an architecture-relevant
implementation, an independent Architecture Guardian review is recommended;
provide the diff, applicable rule IDs, and test results. Guardian review is
read-only and is not a branch-protection or CI gate.

If subagents or agent profiles are unavailable, perform the task directly.
User requests and normal Copilot workflows take precedence over optional
routing suggestions.

## Shared knowledge

Use `AGENTS.md`, `docs/architecture/README.md`,
`docs/architecture/rules.yml`, and the relevant canonical role under
`agent-roles/` as shared instructions. Portable skill source files live in
`.agents/skills/`; matching `.github/skills/` copies are synchronized for
Copilot skill discovery. Select only task-relevant skills; skills complement
agent roles rather than requiring an agent.

Run `python3 tools/validate-agent-framework.py` after changing role, skill,
instruction, or architecture-rule configuration. Follow applicable
architecture/build/test validation from `AGENTS.md`.
