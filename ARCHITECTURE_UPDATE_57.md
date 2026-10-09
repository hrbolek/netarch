# Architecture update 57 — Form node actions and workflow designer

## Form Designer
- Shared `FormNodeActions` kebab menu appears in the structure tree and node-properties card headers; section preview headers also show it.
- Sections expose **Add child** (native HTML dialog), **Edit properties** (anchor to existing property editor) and **Delete** (confirmation).
- Non-container fields offer Edit and Delete; only containers may have children.
- `IFormNodeService.DeleteSubtreeAsync` deletes nested nodes deepest-first inside a transaction after checking draft status and delete rights for each node. A new POST `/digital-processes/FormVersionResponse/designer/delete-form` uses the service.

## Workflow Designer
- GET `/digital-processes/WorkflowVersionResponse/designer/{id}` opens a new editor, linked from the workflow version card.
- The editor displays states with their initial/terminal flags and transitions with source and target names.
- Create, edit and delete state and transition forms use POST endpoints under `/designer/`.
- State deletion is rejected if any transition references it. A transition cannot point to a state outside the current workflow version.
- A published workflow version is read-only; server-side service checks enforce this restriction independently of UI.
- All changes pass through secured services and existing RBAC.

## Verification
- Source-level checks only; this environment does not provide .NET SDK, so compilation must be performed in your development container.
- Suggested command: `dotnet build src/Host/Host.csproj` (adjust to your solution's host project).
