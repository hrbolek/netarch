# Update 77.16 — repeating form sections and GraphQL export

- `DigitalProcessWellKnown.FormNodeTypes.Export`: stable new node-type GUID.
- `DigitalProcessesBootstrapHostedService`: seeds `action.export` type; render registry registers it.
- `FormNodeTreeItem`: existing repeatable section template now exposes predictable instance indexes and OOB swap target.
- `FormVersionDesignerPage` loads `digital-processes-form-preview.js`: dynamic repeat cloning, renumbering and stable `node-{guid}__{occurrence_path}` field names.
- `PreviewFormEndpoints`: preserves values by node identifier plus occurrence path, recursively flattens response arrays, maps only to scoped repeatable sections and rerenders the section's repeat-body via HTMX OOB; exports existing form JSON without importing response.
- `FormVariableResolver`: resolves `$form` and `$form.someCollection` as a structured JSON tree with arrays.
- Export status: success confirmation and dismissible warning for failures; GraphQL error payload handled as an error.

This iteration implements preview-only repeat-instance mapping and export. Permanent Submission persistence is not migrated. A .NET SDK build/integration test is required in the target devcontainer.
