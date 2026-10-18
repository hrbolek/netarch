# ARCHITECTURE_UPDATE_139 — portable templates with binary-only Core

## Goal

Make `/templates/` a self-contained primary reference for AI agents even when the user references the Core infrastructure exclusively through DLL/NuGet packages and does not have the sources for User, Group and Membership.

## Changes (only /templates)

- Added `PORTABLE_CORE_CONTRACT.md`: normative binary-only delivery, public API inspection, parameter/generic validation, acceptance tests.
- Added `REFERENCE_CATALOG.md`: maps user requests to shipped contracts and actual template files; includes four-layer entity delivery checklist.
- Reworked `README.md`, `AGENTS.md`, `DOMAIN_CONTRACT.md`, `ENTITY_CONTRACT.md`, `UI_COMPOSITION_CONTRACT.md`, `INPUT_COMPONENTS_CONTRACT.md`, `VISUALIZATION_CONTRACT.md` so reference Core domains are *historical provenance only* rather than required files.
- Updated all per-scaffold instructions to point to locally packaged recipes and public DLL APIs, not Core module source paths.
- Updated `TEMPLATE_STATUS.md` to distinguish documentation portability from unimplemented generator/runtime verification.

## Constraints retained

Four separate projects (Models / Services / Resolvers / UI), secured server-side Create and CRUD, standard pages/cards/links/field-selectors, transaction safety, RBAC, scoped lookups, nullable FKs, UTC timestamps, and verified build before declaring complete.

## Known limitations

Current `ApplicationTemplate` remains a legacy single-project skeleton and scalar generic input controls may not exist in a specific Core DLL version. No compilation with .NET SDK was possible here; binary-only build smoke test must be performed in the consumer environment. No Core/application code changed.
