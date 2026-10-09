# ZArchitecture 77.3

- Explicit preview identity on each HTMX import button via cascading values and `hx-vals`; `hx-include` still posts current form fields.
- Server logging of autosave, import request identity, configured action, preflight, HTTP status, and persisted result. Logs omit submitted values and secrets.
- More descriptive error when preview identity is absent/invalid.
- Reopen form in Test mode after deploying this build to obtain newly rendered action attributes.
