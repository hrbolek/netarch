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
- `input.checkbox`
- `action.button`

A renderer receives these parameters:

- `FormNodeResponse Definition`
- `SubmissionNodeResponse? Value`

To add a renderer, create a Razor component here and register its key in
`DigitalProcessRendererRegistration.AddDigitalProcessRenderers` or register it on
`IFormNodeRendererRegistry` in the application composition root.
