# ARCHITECTURE_UPDATE_137 — normative templates from Core/User, Group, Membership

## Scope

Changed only `/templates/` and this changelog. No changes to Core implementations, Events, Host, services, database or RBAC behavior.

## Reference audit

Core User/Group/Membership have separate `.Models`, `.Services`, `.Resolvers`, `.UI` projects, secured service contracts, thin Minimal API endpoint extensions, and the standard `PageViewList` / `PageView` / `PageEdit` + domain `MediumCard` / `LargeCard` UI pattern. Membership create uses a permitted container and service-generated security scope. Membership is separate from Role.

## New instructions

- `templates/AGENTS.md`
- `templates/DOMAIN_CONTRACT.md`
- `templates/ENTITY_CONTRACT.md`
- `templates/UI_COMPOSITION_CONTRACT.md`
- `templates/VISUALIZATION_CONTRACT.md`
- `templates/TEMPLATE_STATUS.md`
- updated `templates/README.md` and individual scaffold READMEs.

## Concrete corrections

- Corrected `EntityUITemplate` namespaces and UI wrappers; edit now composes `PageEdit` + domain `LargeCard` + domain editable component.
- `EntityNameLargeCard` composes `EntityNameMediumCard` + `EntityPermissions`.
- `EntityNameMediumCard` declares an explicit actions endpoint placeholder to wire.
- Replaced arbitrary client `rbacobject_id`/Root fallback in the entity-model Create example with a service-validated container request.
- Marked nonconformant legacy generators as partial rather than describing them as complete solutions.

## Verification and remaining work

Documentation and template structure reviewed against reference source. Actual compilation, `dotnet new` generation and app integration tests not performed (dotnet SDK unavailable in this workspace). `ApplicationTemplate` and resolver/service/model generators remain partial and require a subsequent verified refactor to automatically generate a complete four-project domain. No completed visualization generator yet.
