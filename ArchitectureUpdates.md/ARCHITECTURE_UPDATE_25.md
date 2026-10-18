# Architecture Update 25 — Digital Processes

## Goal

Adds the first implementation baseline of the metadata-driven low/no-code digital-process platform under:

```
src/Apps/DigitalProcesses/
  DigitalProcesses.Models/
  DigitalProcesses.Services/
  DigitalProcesses.Resolvers/
  DigitalProcesses.UI/
```

The existing DBLayer/SecurityLayer remain the persistence and authorization foundation. All Digital Processes DB entities implement `ISecuredEntityDBModel` and therefore carry `rbacobject_id` and use the existing row-level authorization query.

## Homogeneous trees

Definition tree:
- `FormDefinitionModel`
- `FormVersionModel`
- `FormNodeModel`
- `FormNodeTypeModel`

Runtime tree:
- `SubmissionModel`
- `SubmissionNodeModel`

There is no separate Section/Item persistence model. Sections, fields, buttons/importers and future components are all `FormNodeModel`; their semantics come from `FormNodeTypeModel` and `renderer_key`.

### Materialized paths

`FormNodeModel.path` and `SubmissionNodeModel.path` are mandatory ID-based materialized paths:

```
/<node-guid-N>/<child-guid-N>/...
```

Paths are created only in Services. API callers cannot provide a path. Moving a form node rewrites the complete authorized subtree. Submission snapshots cloned on workflow transition receive completely new IDs and therefore completely new materialized paths.

## Cardinality

`FormNodeModel` carries:
- `min_occurs`
- `max_occurs`

A request start materializes only the minimum required tree. Optional/repeating nodes remain sparse until explicitly created.

`SubmissionNodeModel` carries `occurrence` and has a uniqueness constraint over:

```
(submission_id, parent_id, form_node_id, occurrence)
```

## Values

`SubmissionNodeModel.value_json` contains the JSON value of value-bearing nodes. `FormNodeTypeModel.can_have_value` controls whether a value is allowed. JSON is syntax-validated before update.

## Workflow

Versioned workflow entities:
- `WorkflowDefinitionModel`
- `WorkflowVersionModel`
- `WorkflowStateModel`
- `WorkflowTransitionModel`

Transitions are explicit graph edges with source/target state, optional note requirement and optional `condition_query` / `action_query` declarations.

## Process definition and runtime request

`ProcessDefinitionVersionModel` binds one concrete `FormVersionModel` to one concrete `WorkflowVersionModel`.

`RequestModel` points to:
- process definition version,
- current workflow state,
- current active submission.

`RequestTransitionHistoryModel` stores:
- transition,
- from/to states,
- archived submission,
- actor user,
- note,
- timestamp.

## Immutable history

Workflow transition is one application transaction:
1. validate current request and transition,
2. evaluate optional transition condition through `IDigitalProcessIntegration`,
3. archive the active submission,
4. clone its complete node tree with new IDs and ID-based paths,
5. append transition history,
6. point Request to target state and new active submission.

Archived submissions reject value changes and structural additions.

## Definition publishing

Definition versions have draft/published lifecycle:
- published form versions are structurally immutable,
- published workflow versions are structurally immutable,
- workflow publish requires exactly one initial state,
- process version publish requires published form + workflow versions,
- requests can start only from published process versions.

## Integration boundary

Digital Processes does not depend on a concrete JSONata/GraphQL transport. `IDigitalProcessIntegration` is the boundary for the sidecar. The default `UnconfiguredDigitalProcessIntegration` fails closed if a declared query is executed. Replace this DI registration with the actual sidecar adapter.

GraphQL-like queries, aliases and JSONata transformations therefore remain external to the form engine; FormNode `configuration_json` and workflow query declarations reference/contain those integration definitions as metadata.

## UI extensibility

`FormNodeTypeModel.renderer_key` is resolved by `IFormNodeRendererRegistry` in `DigitalProcesses.UI`.

`FormNodeRenderer.razor` uses `DynamicComponent`, so a new visual component can be added by registering a renderer without changing the form engine.

## Endpoints

Digital Processes are exposed under `/digital-processes` and still use the same HTML/JSON content negotiation infrastructure (`ApplyViewModel` / `ApplyJsonModel`). All entity DTOs are registered in the central `EntityRouteRegistry` with app `digital-processes`, so generic `<EntityLink>` works for them.

Important runtime endpoints include:
- `POST /digital-processes/RequestResponse/start`
- `POST /digital-processes/RequestResponse/transition/{id}`
- `GET /digital-processes/RequestResponse/detail/{id}`
- `GET /digital-processes/FormVersionResponse/tree/{id}`
- `PUT /digital-processes/FormNodeResponse/move/{id}`
- `GET /digital-processes/FormNodeResponse/subtree/{id}`
- `GET /digital-processes/SubmissionResponse/tree/{id}`
- `PUT /digital-processes/SubmissionNodeResponse/value/{id}`

## Database note

This update adds new EF entities/tables. The project still uses `EnsureCreated` rather than migrations, so an existing SQLite database will not be migrated automatically. For the current development workflow the database must be recreated. A later update should move schema evolution to EF migrations.

## Static verification

- All `src/*.csproj` project references resolve.
- New C# files pass basic structural/braces checks.
- `SecurityLayer/SecurityLayer.cs` SHA-256 is identical to Update 24; the optimized runtime authorization query was not changed.
- `dotnet` is not available in the generation environment, therefore this update has not been compiled here.
