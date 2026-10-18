# Retired ArchitectureInspector

The attribute-driven `Architecture.Auditor` and its test project have been
removed at the user's request. Production and template architecture annotations,
Razor attribute directives and injected attribute-library references have also
been removed. Existing DTO names, generic interfaces and DI behavior are
unchanged.

The auditor read architecture annotations to classify declarations and public
signatures; it was therefore not a reliable application gate after annotation
removal. Its latest implementation also contained inferred-contract checks and
the `ARCH201` project-reference policy. Those checks were part of the same
auditor and are no longer run after the explicit decision to remove the entire
auditor; no replacement architecture audit or equivalent coverage is claimed.
Application tests and NuGet dependency/security checks remain active.

The [compatibility validation script](../audit-architecture.sh) now builds and
tests the application and writes a direct/transitive NuGet vulnerability report
to `artifacts/nuget-audit.json`. It does not generate architecture findings.
Historical architecture reports are not evidence of current validation.

## Remaining library

[Architecture.Attributes](src/Architecture.Attributes/Architecture.Attributes.csproj)
and its enum/attribute types are retained for review, but no application,
template or tool project references them anymore. The remaining
[standalone solution](ArchitectureInspector.slnx) contains only this unused
library; it is not part of CI or the application build.

**Proposed follow-up:** remove this standalone solution and the unused library
together, once removal of their externally consumed public types is approved.
No other application contracts or used types need to be deleted.
