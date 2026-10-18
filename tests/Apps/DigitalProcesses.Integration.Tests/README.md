# DigitalProcesses integration tests

Only DigitalProcesses HTTP behavior belongs here. Uses shared test Host with a fresh SQLite file per test; no provider-specific SQL. The initial suite covers absent/inaccessible entities, policy lookup of unknown versions, and HTTP route contracts. It does **not** yet prove valid Request -> Submission -> transition or archival and per-node Deny filtering with seeded data.

### Stage 3 – HTTP authorization boundary (additional 6 cases)

`DigitalProcessesAuthorizationHttpTests` exercises actual Host/Resolvers/Services/AuthorizationLayer/SQLite calls:
- an actor sees no Request, Submission or SubmissionNode in an empty isolated database (three cases);
- an unknown RBAC operation cannot be used to expand a Request/SubmissionNode list (two cases);
- OpenAPI declares the actual HTTP verbs for starting a request, transitioning, updating a node value and reading its subtree (one case).

**Not yet covered:** a *positive* seeded process lifecycle, Request → Submission → transition with archived snapshot and preservation of hidden node values. Those need a published ProcessDefinitionVersion, Workflow/Form versions, roles, access bindings and a state policy. Route registration or empty-database tests do not substitute for that fixture.

## Etapa 3 — doplněné HTTP kontrakty

`DigitalProcessesLifecycleContractTests` ověřují GUID routing, rozdílné HTTP metody, izolaci testovacích Hostů a zápisové endpointy ve Swaggeru. **Neověřují** úspěšný přechod procesu. Pozitivní E2E fixture musí obsahovat publikovanou ProcessDefinitionVersion, FormVersion, WorkflowVersion/States/Transition, role a účinná RBAC oprávnění. Teprve následně je možné testovat StartAsync -> TransitionAsync, neměnný archiv starého Submission a utajení FormNode dat.

## Nové scénáře

Viz [SCENARIO_PLAN.md](SCENARIO_PLAN.md). DP01–DP03 pokrývají neúspěšné příkazy bez viditelného zápisu. Pozitivní lifecycle DP04–DP15 zůstává explicitně nepokrytý.

## Entity-first organization checks (no database required)

`EntityOrganizationTests` validates entity-owned folders and namespaces in Models,
Services and Resolvers, all 19 database model registrations, and the unchanged
PostgreSQL schema fingerprint. Schema generation is offline and never opens a
database connection. `UiOrganizationTests`, `EntityCardsTests`, `EntityPagesTests`
and `DefinitionVersionsPageTests` validate UI ownership and rendering, including
all 12 entity-specific creation forms and their dependent lookup configuration.

```sh
dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj --no-restore --filter 'FullyQualifiedName~EntityOrganizationTests|FullyQualifiedName~UiOrganizationTests|FullyQualifiedName~EntityCardsTests|FullyQualifiedName~EntityPagesTests|FullyQualifiedName~DefinitionVersionsPageTests'
```

## Auxiliary-service composition

`ServiceCompositionTests` checks that all 16 secured entities have exactly one
correctly named service, all eight auxiliary services use composition without
persistence/security-layer injection, and generic entity-service aliases share
the named service's scoped instance. Its DI test uses the isolated test Host.

`ComposedServiceBehaviorTests` exercises owner-authorized RTF upload/revisions,
invalid bindings without partial writes, template cascade deletion, applicant
policy updates, approved AI trees, published-version rejection and denied actors.
An explicit Read-only role checks Read versus List/Update semantics; AI proposals
use an in-process HTTP replacement rather than any external agent.
`EndpointLayeringTests` accepts secured entity services or auxiliary services
composing secured entity contracts, never direct resolver persistence.
These integration tests use the configured `postgres_test` server and fresh
disposable databases, never the development database.

## FormVersion designer

`FormVersionDesignerPageTests` renders the renamed designer in draft, published,
test and subtree modes; verifies forms, toolbar URLs, qualified ancestor paths
and external script references; and checks tree ordering and orphan visibility.
`FormNodeEditorTests` also checks that all three extracted scripts are served as
JavaScript, alongside the existing real HTTP editor and insertion scenarios.
