# DigitalProcesses form renderers

Concrete form-node renderers belong in this directory.

UI files are grouped by entity, then by role. Page components that wrap `Page`
directly or through `PageView`/`PageViewList` belong in their entity's `Pages`
directory. For example, the form designer belongs in
[`FormVersion/Pages`](../../FormVersion/Pages), while node editor components
belong in [`FormNode/Components`](../Components). The existing form designer page
also serves the node-subtree editor route; it is not duplicated.
Workflow pages and graph components belong in
[`WorkflowVersion`](../../WorkflowVersion). Shared presentation components,
including `DefinitionVersionsTable`, belong in
[`Shared/Components`](../../Shared/Components). Component namespaces mirror
these paths. Renderer registration and the renderer registry live alongside the
concrete renderers in this directory.
Shared has no page entry points. Each entity owns its list, model and create pages;
common creation content and form-field configuration live in Shared/Components.
Process and workflow version lists use separate page models and direct resolver
bindings, sharing only the versions table.
Each entity also owns its typed MediumCard, LargeCard and ModelCard, and related
entity links where applicable. Display fields and domain navigation are explicit;
shared components do not inspect DTO types or properties to select entity behavior.

## Universal node editor and insertion wizard

The existing FormVersion designer remains the editor of the whole form (there is
no persisted `form` FormNode type). `FormNodeResponse/editor/{id}` opens the same
`FormVersionDesignerPage` for a readable section or leaf subtree. `root_node_id` is only
the root of that view; the stored node keeps its parent, UUID, path and version.
Optional `ancestor_nodes` supplies readable ancestors only for qualified key-path
display, not as editable nodes. An unreadable ancestor produces an explicit
unavailable-path message rather than a misleading relative selector.
`FormNodeProperties`, `FormEditorCanvas`, `FormNodeTreeItem` and `FormNodeRenderer`
are shared, rather than implementing another renderer/property editor.

The page only selects design/preview/published mode and composes the editor.
FormVersion components own its header, workspace, preview, AI and template cards.
FormNode components own the structure, library, add card and edit/move/add dialogs.
`FormNodeEditorView` centralizes tree order, container/type choices, subtree URLs
and qualified paths including readable ancestors. The response DTO remains
`FormDesignerPageResponse` for HTTP compatibility; all form and subtree routes
bind directly to the renamed page.

Designer JavaScript is served as three versioned static assets:
`digital-processes-form-ai-designer.js`, `digital-processes-node-actions.js` and
`digital-processes-form-preview.js`. Their existing DOM selectors, global repeat
helpers, AI requests, preview autosave and HTMX events are preserved.

Choose **Vložit prvek** in the form toolbar or a container's node menu:

1. Choose Library or Basic renderers. The library queries existing readable
   FormNodes, not a template table. It supports case-insensitive key/label/
   description and configuration-text filters, exact type/renderer/source-version
   filters, and stable server paging. Counts and child counts are RBAC-filtered.
2. Copy a node alone or its readable subtree, or select an actual registered
   renderer using the existing `FormComponentCatalog` example. The gallery renders
   real Razor components. Custom registered types without a built-in example
   start with an empty configuration object.
3. Edit the temporary tree with the same properties component and tree renderers.
   Changes update the preview after a short debounce (Refresh is also available).
   Add/remove children, select their parent and position, edit JSON, and simulate
   readonly/disabled/validation states. Input values belong only to the preview.
   Detached GraphQL search uses the same query evaluation and fixed
   `data.choices/id/label` contract as persistent search, including form variables.
4. Choose target container/position and confirm. New UUIDs are allocated again;
   GUID references (D/N forms, including JSON keys and embedded `node-...` names)
   within the copied configuration are remapped. Qualified `$.form...` selectors
   into the copied subtree are rebased for the detached preview, node renames,
   reparenting and final destination. References outside the copied subtree are
   not rewritten. This is text-based rebasing of canonical selectors, not a
   general JSONata/JavaScript parser; dynamically constructed or bracket-notation
   selectors require review.

The draft exists only in the dialog's JavaScript closure. There is no draft
database entity, server cache, browser storage, or cross-user session token.
Closing/cancelling/back-to-sources drops the draft and test values; page reload
also discards them. Preview HTTP calls validate but do not persist anything.
Import/export/document actions are deliberately unavailable in detached previews
to prevent side effects; input/search/select/checkbox interactions use the actual
renderer components. Readonly for select/checkbox/search uses disabled controls,
because those HTML controls do not support `readonly`.

Confirmation rechecks target Create, draft/published state, readable types,
structure, limits and keys. Following siblings are shifted transactionally and
require Update on those rows. An authorization failure rolls back the insertion.
All copies inherit only the target container's RBAC context and current actor/time;
source audit data and `own_security_scope` are not copied. After successful insertion,
the wizard returns to the whole target form's designer, even when opened from a
subtree editor. Its return URL is supplied by the rendered wizard, and the editor
script URL includes the UI assembly build identifier to invalidate cached scripts
after deployment. The subtree editor omits the legacy Library, Add node and
document-template cards; inserting nodes remains available through the wizard.
GraphQL search options use dropdown stacking order above subsequent form controls.
Concurrent conflicting insertions may need retry after reload;
there is no new draft/version concurrency token or multi-user merge protocol.

### Additional FormNode endpoints

All paths below start with `/digital-processes/FormNodeResponse`.

| Method | Path | Contract |
| --- | --- | --- |
| GET | `/editor/{id}?test=true` | Existing designer model plus optional `root_node_id`; HTML or JSON |
| GET | `/library` | `search`, `configuration`, `node_type_id`, `renderer_key`, `form_version_id`, `offset`, `limit` (1..50); `{items,total,offset,limit}` |
| GET | `/renderer-examples` | Real Razor gallery (HTML) or existing example metadata (JSON) |
| POST | `/draft` | Exactly one of `source_node_id` / `node_type_id`, optional `include_children`; returns detached `{root_id,nodes}` |
| POST | `/draft/workspace` | `{target_version_id,draft,selected_id?}`; shared property/tree workspace (HTML) or validated preview model (JSON) |
| POST | `/draft/preview` | Same input; real interactive preview without property controls |
| POST | `/draft/search` | `{workspace,node_id,pattern,values?}`; normal fixed GraphQL choices contract, no saved values |
| POST | `/insert-draft` | `{form_version_id,parent_id?,position,draft}`; inserted nodes in parent-first order |

Draft nodes contain only `id` (temporary), `parent_id`, `key`, `node_type_id`,
`label`, `description`, `position`, `min_occurs`, `max_occurs` and
`configuration_json`. UUIDs are never trusted as destination IDs. No preview
value, owner, audit field, RBAC object or own-security-scope flag is part of this
contract. Optional `reference_paths` maps temporary IDs to their last qualified
paths, allowing stateless rename/reparent rebasing. Prepare/workspace return the
normalized draft; clients should keep that returned draft for subsequent edits.
When adding/removing nodes the server refreshes this map. Omitting it means the
submitted keys already describe the paths used by the configuration.
Bounds are 400 nodes, depth 20, configuration 64000 characters per node and
occurrence maximum 10000. Invalid bodies return 400, unreadable sources/targets
404, denied Create 403 and immutable versions 409. Source readability is checked
when preparing; inserting a detached draft does not reuse source authorization.

The database stores a `FormNodeType.renderer_key`. The UI resolves that key through
`IFormNodeRendererRegistry` and `FormNodeRenderer.razor` renders the selected Razor
component using `DynamicComponent`.

Built-in keys:

- `section`
- `input.text`
- `input.number`
- `input.date`
- `input.time`
- `input.checkbox`
- `input.select`
- `input.graphql-search`
- `input.textarea`
- `display.url`
- `action.import`
- `action.document`
- `action.export`
- `action.button`

## Sections, tabs and repeated tables

Only `section` nodes may be created directly below the form root. The same rule
applies to schema/bulk imports, subtree imports and moves. Existing legacy root
fields are not automatically moved or deleted.

Top-level sections are always rendered as accessible tabs in the designer canvas,
form preview and process test form. The first tab starts active; switching tabs
hides the other panels without removing their controls or clearing their values.
Arrow keys, Home and End also select tabs.

Nested sections default to the stacked card layout. A nested section with
`max_occurs > 1` and no child sections can opt into a table:

```json
{
  "renderer_key": "section",
  "min_occurs": 1,
  "max_occurs": 10,
  "configuration": { "visualization": "table" },
  "children": {
    "description": { "renderer_key": "input.text" },
    "quantity": { "renderer_key": "input.number" }
  }
}
```

Use `"visualization": "stacked"` (or omit it) for the ordinary layout.
`"tabs"` is accepted only for top-level sections, where tabs are automatic.
The designer exposes the layout in section properties and keeps it in the same
configuration object as other options. Invalid table layouts are rejected by
the services, including updates which retain previously stored child sections.

A table row represents one section occurrence; columns follow child-node order.
Input cells show their value until clicked or keyboard-activated, then expose
the existing input renderer. Leaving a cell returns to value display; normal
change/autosave handling is retained. Search cells display the selected JSON
object's `label`, never its `id` or serialized object. Non-input action/display
renderers remain directly accessible. Preview Add/Remove occurrence controls
operate on complete rows and keep occurrence-qualified field names.

A renderer receives these parameters:

- `FormNodeResponse Definition`
- `SubmissionNodeResponse? Value`

`input.graphql-search` configuration contains a GraphQL `query`. After three characters,
the UI waits 400 ms before requesting results and always supplies the trimmed input as
the GraphQL variable `fragment`. The query must alias its result collection to `choices`
and its selected value and display fields to `id` and `label`, respectively. These names
are fixed in the implementation and are not configurable. Selecting a result stores
`{ "id": ..., "label": ... }`. The query is executed by the server only after the caller
has Read access to the configured form node. Optional
`variables` may add constant values or `$.form` / `$.form.<path>` expressions resolved
from current form values. The `fragment` key is reserved for the search input.
Each configured key must correspond to a variable declared by the GraphQL query. For example:

Configuration is a JSON object, not a raw GraphQL document. Newlines inside its
`query` string must be escaped as `\n`; literal line breaks are allowed only outside
JSON strings. Invalid configuration is rejected when saving. Legacy malformed
search configuration returns HTTP 422 with a diagnostic shown by the search input.

```json
{
  "query": "query userread($fragment: Str) { choices: userList(where: { email: { _ilike: $fragment } }) { id label: email } }",
  "minLength": 3,
  "variables": {}
}
```

Alias the selected collection and fields exactly as `choices`, `id` and `label`, as in
the example. The input is passed as supplied; for `_ilike` substring matching,
the input variable should contain `%` wildcards (for example `%ichal%`). Add any
additional GraphQL variables to `variables` and declare/use them in the query. Values
can be constants (for example `"status": "active"`) or form expressions (for example
`"projectCode": "$.form.basic_information.project_code"`).

The UI Host serves the renderer script from
`/_content/DigitalProcesses.UI/digital-processes-search.js` through static-file
middleware. Search initializes in form and process previews, after HTMX swaps, and
in newly added repeated sections. Only the selected hidden JSON value is persisted,
not the visible search text.

The JSONata `userList` mapping in `config/endpoints.json` explicitly forwards query
arguments to REST; object-valued `where` and `order_by` are JSON-serialized.
After changing this configuration, reload/restart the JSONata service before testing.

To add a renderer, create a Razor component here and register its key in
`DigitalProcessRendererRegistration.AddDigitalProcessRenderers` or register it on
`IFormNodeRendererRegistry` in the application composition root.
