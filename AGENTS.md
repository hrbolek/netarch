# ZArchitecture development instructions

- Custom agents and Agent Skills are optional conveniences.
  Developers may build, test, review and maintain the repository with ordinary
  IDEs, CLI tools, Git workflows, other assistants or no AI assistant. No agent
  invocation, agent approval, paid AI service or LLM credential is required.
- When using AI assistance, treat authority in this order: (1) explicitly
  accepted architectural decisions, (2) verified architectural contracts and
  invariants, (3) current source and tests, (4) established implementation
  conventions, and (5) agent recommendations. Report conflicts instead of
  silently choosing an interpretation. The stable rule identifiers and
  evidence links are maintained in
  [`docs/architecture/rules.yml`](docs/architecture/rules.yml).
- This `AGENTS.md` is the shared repository instruction entry point.
  Canonical role specifications live in `agent-roles/`, and portable Agent
  Skills live in `.agents/skills/`. Provider adapters such as `.github/agents/`
  should stay thin and refer back to those sources. Read only roles/skills
  relevant to the task; orchestration guidance is advisory, not a repository
  development gate.
- Architecture annotations and the attribute-driven auditor have been retired. Do not add architecture annotations or production references to the unused `Architecture.Attributes` library.
- Preserve existing database, DTO and service generic contracts and enforce boundaries through the existing types, DI registration and tests. Follow [`templates/DOMAIN_CONTRACT.md`](templates/DOMAIN_CONTRACT.md); do not classify by suffix or folder alone.
- Run `./tools/audit-architecture.sh` after architecture changes or adding an entity. This compatibility entry point now builds/tests `SignFakeArchitecture.sln` and audits direct/transitive NuGet vulnerabilities into `artifacts/nuget-audit.json`; it does not run an architecture auditor. Build/test failures remain failures, and NuGet security checks remain enabled.
