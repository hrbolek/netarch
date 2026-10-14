# DigitalProcesses form renderers

Concrete form-node renderers belong in this directory.

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
