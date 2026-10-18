# ZArchitecture 61 — UI cards and designer previews

- DigitalProcessMediumCard and DigitalProcessLargeCard are reusable wrappers based on Core/Base `MediumCard` and `LargeCard`; generic model view/list pages now use them. LargeCard has a typed ChildContent slot; the previous entity-specific action card remains its default central content.
- FormNode configuration editor offers renderer-oriented JSON placeholder and help text. The configuration is not persisted unless the user submits a value.
- FormNodeTreeItem supports adding/removing occurrences for sections with `max_occurs > 1` in browser test rendering. This clones browser inputs for inspection only; it does not create `SubmissionNode` records. Nested runtime persistence, unique field identity, and server-side validation are separate implementation work.
- WorkflowDesignerPage includes a read-only interactive workflow preview that starts at the initial state, displays outgoing transitions, simulates a click and allows reset; neither conditions nor access rules nor actions are executed. It is not a Request state transition API.
- No .NET SDK available in the execution environment; compilation must be verified in a devcontainer.
