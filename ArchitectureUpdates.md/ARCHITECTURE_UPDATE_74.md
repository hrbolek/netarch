# ZArchitecture74 — Configurable GraphQL import and variable preflight

- `src/Host/appsettings.json` supplies default backend values; `appsettings.Development.json` enables GraphQL in Development. Override with `DigitalProcesses__DataBackend__Provider` and `DigitalProcesses__DataBackend__Endpoint`. Adjust `/graphql` to the actual server route.
- `FormBackendOperationRequest.variables` supports JSON configuration for arbitrary GraphQL variables, without removing old positional callers.
- `FormVariableResolver.Resolve` evaluates `$form.*`, `$process.*`, `$context.*` in `PreprocessAsync` before `FetchAsync` and keeps JSON types. `$$` escapes literal `$`. Missing references raise errors.
- `ExternalDataImportSection.ConfigurationJson` configures `label`, `query`, `variables`; it flattens the GraphQL `data` object into editable fields by path. Demo action imports `whoami.hostname`.
- `RuntimeValues` are deliberately not inferred from client input; the host must supply trusted context values when wiring production forms.
- Scope: demonstration component is still not persisted in FormNode or Submission; editing fields of an existing form and saving a Submission needs subsequent integration.
- No .NET SDK available here: build and browser interaction need local verification.
