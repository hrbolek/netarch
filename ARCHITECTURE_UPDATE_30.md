# Architecture Update 30

## Digital Processes create UI

Digital Processes now exposes real browser-facing create operations in addition to the existing JSON API.

A reusable Razor page was added:

- `src/Apps/DigitalProcesses/DigitalProcesses.UI/Pages/DigitalProcessCreatePage.razor`
- `src/Apps/DigitalProcesses/DigitalProcesses.UI/Pages/DigitalProcessCreatePageModel.cs`

Create endpoint mappings and HTML form-to-command conversion are isolated in:

- `src/Apps/DigitalProcesses/DigitalProcesses.Resolvers/Resolvers/DigitalProcessCreateEndpoints.cs`

The existing typed command DTOs and Services remain authoritative. HTML forms only convert `application/x-www-form-urlencoded` input into those existing commands.

### Browser create endpoints

The following entities now have `GET .../create` and `POST .../create-form` operations:

- FormDefinition
- FormVersion
- FormNodeType
- FormNode
- WorkflowDefinition
- WorkflowVersion
- WorkflowState
- WorkflowStateAccessRule
- WorkflowTransition
- ProcessDefinition
- ProcessDefinitionVersion
- Request (starts a request)

System-generated runtime/audit entities are deliberately not given an independent create page:

- Submission is created by request start / workflow transition.
- RequestTransitionHistory is created by workflow transition.
- SubmissionNode remains a contextual runtime operation, e.g. for adding an occurrence of a repeatable node.

### Navigation

Digital Processes navigation now contains `New ...` entries for creatable entities.
The generic `DigitalProcessListPage<TModel>` also renders a `Create` button when the model represents a creatable entity.

### RBAC input

Create forms preserve the capabilities of the JSON command DTOs and expose optional `rbacobject_id` where supported.

`ProcessDefinition` requires an explicit `rbacobject_id`. It must reference the RBAC Group used as the security root of the digital process, matching the existing `ProcessDefinitionService` invariant.

### OpenAPI

The new browser create endpoints also use `.WithDescription("""...""")` metadata so they are visible and understandable in the generated OpenAPI/Swagger description.

## Database

No database schema change is introduced by Update 30.
