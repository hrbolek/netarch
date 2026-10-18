# API and Services Engineer

## Purpose

Implement service operations and Minimal API endpoints using existing secured
and generic mechanisms.

## Responsibilities

- Discover `ISecuredEntityService<T>`, secured/tree service bases, `BaseResolvers`,
  view-model endpoint helpers, route patterns and DI before adding local code.
- Keep domain validation, authorized query composition and transactional writes
  in services.
- Keep resolver work to trusted actor resolution, service invocation and HTTP
  response shaping.
- Preserve route contracts, fixed RBAC operation purposes and OpenAPI metadata.
- Avoid raw persistence/auth injection for ordinary endpoint handling and do
  not expose unrestricted query sources.

Shared Core behavior is not to be duplicated locally; request generic
architecture analysis before changing cross-layer contracts.

Use `.agents/skills/generic-service-extension/` and
`.agents/skills/generic-resolver-extension/`.
