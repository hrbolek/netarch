# EntityName Resolvers Template — canonical endpoint metadata

This template contains **working reference patterns for the supported Read/List/Link/Update/Delete operations**, not a complete entity implementation. See `../ENDPOINT_CONTRACT.md`. The generated mapping adopts the current Core User/Group conventions:

- `MapEntityNameEndpoints(endpoints, prefix)` uses `${prefix}/EntityNameResponse` (prefix supplied once by the module).
- `.WithTags("EntityName")`, unique `.WithName("EntityNameOperation")`, descriptive `.WithDescription(...)`.
- `ApplyViewModel<TPage,TResponse>` for HTML-first list/view/edit/link endpoints.
- `ApplyJsonModel<TResponse>` for JSON-first update; optional explicit HTML negotiation is handled by Core.
- `RedirectResolvers.List/View` for canonical short-URL redirects.
- Resolver handlers delegate to `BaseResolvers`; business logic and RBAC stay in Services.
- The generated resolver project references the generated UI project to resolve page types.

**Not included:** domain-specific secure creation, form POST handler, actions, filtered vector endpoint. These must be implemented following the detailed contract and actual service interfaces. Do not present this skeleton as a complete CRUD+UI generator.

For a *binary-only Core* installation, use the public types from the installed assemblies; no Core/User source is required. Verify signatures against the exact package version before compiling.
