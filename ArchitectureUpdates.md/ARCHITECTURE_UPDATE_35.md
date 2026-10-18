# Architecture Update 35 — server-derived CREATE authorization

## Goal

CREATE is authorized against an already existing trusted RBAC scope. A public create request must never be able to choose the `rbacobject_id` of the object being created.

The runtime authorization engine is unchanged:

`EffectiveAccess + RoleTypeOperation -> allowed operation on rbacobject_id`

The difference is only how the authorization target is resolved before a new entity exists.

## Creation modes

### contained

The new object is created inside an existing domain object. `create` is checked on that existing parent/container and the new object's security scope is derived by the service.

Examples: Membership in Group, FormVersion in FormDefinition, WorkflowState in WorkflowVersion.

### typed

A root object has no domain parent. `create` is checked on a trusted type/catalog scope. The scope is selected by server code, never by a request-provided RBAC id.

Root Group is authorized by GroupType. DigitalProcesses root definitions use well-known creation catalog groups seeded by `DatabaseInitializer`.

### internal

Infrastructure/runtime entities which must only arise as a consequence of another operation do not expose public create endpoints.

Examples: Submission, RequestTransitionHistory, EffectiveAccess, RbacAccessBinding.

## Public API matrix

| Entity | Public create operation | Authorization target |
|---|---|---|
| User | `POST /architecture/UserResponse/create-contained` | target Group; Membership is created atomically |
| Membership | `POST /architecture/MembershipResponse/create-contained` | target Group |
| Role | `POST /architecture/RoleResponse/create-contained` | target Group |
| Group | `POST .../create-contained` | parent Group |
| Group | `POST .../create-typed` | GroupType |
| GroupType | none | bootstrap/SystemData only |
| RoleType | none | bootstrap/SystemData only |
| Operation | none | initializer only |
| RoleTypeOperation | none | initializer/policy only |
| EffectiveAccess | none | materialized projection only |
| RbacAccessBinding | none | internal workflow/security operation only |
| FormDefinition | `POST .../create-typed` | FormDefinition creation catalog |
| FormVersion | `POST .../create-contained` | FormDefinition |
| FormNodeType root | `POST .../create-typed` | FormNodeType creation catalog |
| FormNodeType child | `POST .../create-contained` | parent FormNodeType |
| FormNode root | `POST .../create-in-version` | FormVersion |
| FormNode child | `POST .../create-in-node` | parent FormNode |
| WorkflowDefinition | `POST .../create-typed` | WorkflowDefinition creation catalog |
| WorkflowVersion | `POST .../create-contained` | WorkflowDefinition |
| WorkflowState | `POST .../create-contained` | WorkflowVersion |
| WorkflowTransition | `POST .../create-contained` | WorkflowVersion |
| WorkflowStateAccessRule | `POST .../create-contained` | WorkflowState |
| ProcessDefinition | `POST .../create-typed` | ProcessDefinition creation catalog |
| ProcessDefinitionVersion | `POST .../create-contained` | ProcessDefinition |
| Request | `POST .../start` | ProcessDefinition security scope |
| Submission | none | created by request start/transition |
| SubmissionNode root | `POST .../create-in-submission` | active Submission |
| SubmissionNode child | `POST .../create-in-node` | parent SubmissionNode |
| RequestTransitionHistory | none | created by transition |

HTML `GET /create` + `POST /create-form` endpoints are UI adapters. They call the same secure service methods; they do not implement a second create path.

## Security implementation

`CreationScopeGuard` centralizes the trusted scope resolution used by Services. It resolves an existing secured entity with `SecurityOperations.Create`, or a well-known server-side catalog scope.

The old generic `BaseService.CreateAsync` / `BaseResolvers.Create` path was removed. Legacy public create DTOs carrying optional `rbacobject_id` were removed as well. This makes the secure creation model structural rather than dependent only on the currently mapped endpoints.

Services create or derive `rbacobject_id` themselves:

- User gets its own stable id as `rbacobject_id`; initial Membership is created in the authorized Group.
- Group gets its own RBAC id.
- DigitalProcesses root definitions and FormNodeTypes get dedicated child RBAC Groups.
- contained version/state/rule entities share/derive their parent scope.
- runtime own-security scopes are created only by the corresponding domain service.

When a create operation introduces a new RBAC Group, `EffectiveAccess` is rebuilt in the same transaction so inherited access becomes effective immediately.

## Seeded typed creation scopes

`DatabaseInitializer` idempotently creates these Group scopes beneath the technical root:

- Form Definition creation scope
- Workflow Definition creation scope
- Process Definition creation scope
- Form Node Type creation scope

They are created **after** the initial SystemData import decision. Therefore their existence cannot make a fresh database look already imported.

If startup restored `dump.json`, GitHub/SystemData import remains disabled; the local initializer can only add missing technical creation scopes and rebuild local security projections.

## Legacy RBAC endpoint surface

`MapRbacEndpoints` is now read-only (`health`, `access/can`, `permissions`). Its old mutation mappings were removed so accidentally mounting that legacy endpoint group cannot expose an alternative create/upsert authorization model.

## Razor UI

Create Razor pages no longer ask the user for `rbacobject_id`.

- User create asks for the target Group.
- Group create chooses contained vs typed by presence of a parent Group.
- DigitalProcesses uses the common `DigitalProcessCreatePage`; parent/type/version identifiers are domain inputs and the resolver dispatches to the corresponding secure service operation.
- bootstrap-only RoleType no longer has a create Razor page.

The JSON API is intentionally stricter than the convenience HTML dispatcher. Separate endpoint names make the creation scope explicit in Swagger.
