# ZArchitecture development instructions

- Architecture annotations and the attribute-driven auditor have been retired. Do not add architecture annotations or production references to the unused `Architecture.Attributes` library.
- Preserve existing database, DTO and service generic contracts and enforce boundaries through the existing types, DI registration and tests. Follow [`templates/DOMAIN_CONTRACT.md`](templates/DOMAIN_CONTRACT.md); do not classify by suffix or folder alone.
- Run `./tools/audit-architecture.sh` after architecture changes or adding an entity. This compatibility entry point now builds/tests `SignFakeArchitecture.sln` and audits direct/transitive NuGet vulnerabilities into `artifacts/nuget-audit.json`; it does not run an architecture auditor. Build/test failures remain failures, and NuGet security checks remain enabled.
