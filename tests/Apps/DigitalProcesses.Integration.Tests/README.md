# DigitalProcesses integration tests

Only DigitalProcesses HTTP behavior belongs here. Uses shared test Host with a fresh SQLite file per test; no provider-specific SQL. The initial suite covers absent/inaccessible entities, policy lookup of unknown versions, and HTTP route contracts. It does **not** yet prove valid Request -> Submission -> transition or archival and per-node Deny filtering with seeded data.

### Stage 3 – HTTP authorization boundary (additional 6 cases)

`DigitalProcessesAuthorizationHttpTests` exercises actual Host/Resolvers/Services/SecurityLayer/SQLite calls:
- an actor sees no Request, Submission or SubmissionNode in an empty isolated database (three cases);
- an unknown RBAC operation cannot be used to expand a Request/SubmissionNode list (two cases);
- OpenAPI declares the actual HTTP verbs for starting a request, transitioning, updating a node value and reading its subtree (one case).

**Not yet covered:** a *positive* seeded process lifecycle, Request → Submission → transition with archived snapshot and preservation of hidden node values. Those need a published ProcessDefinitionVersion, Workflow/Form versions, roles, access bindings and a state policy. Route registration or empty-database tests do not substitute for that fixture.

## Etapa 3 — doplněné HTTP kontrakty

`DigitalProcessesLifecycleContractTests` ověřují GUID routing, rozdílné HTTP metody, izolaci testovacích Hostů a zápisové endpointy ve Swaggeru. **Neověřují** úspěšný přechod procesu. Pozitivní E2E fixture musí obsahovat publikovanou ProcessDefinitionVersion, FormVersion, WorkflowVersion/States/Transition, role a účinná RBAC oprávnění. Teprve následně je možné testovat StartAsync -> TransitionAsync, neměnný archiv starého Submission a utajení FormNode dat.

## Nové scénáře

Viz [SCENARIO_PLAN.md](SCENARIO_PLAN.md). DP01–DP03 pokrývají neúspěšné příkazy bez viditelného zápisu. Pozitivní lifecycle DP04–DP15 zůstává explicitně nepokrytý.
