# Architecture update 66 — API representation metadata and mutation UI

- `ProducesEntity<T>` now lives in `ViewModelEndpointExtensions` and declares both JSON and HTML 200 responses. `ApplyRepresentation<TView,TModel>` invokes it automatically. JSON-only `ApplyJsonModel<T>` remains JSON-only.
- Removed the duplicate standalone metadata extension class to prevent ambiguous extension resolution.
- Extracted `DigitalProcessMutationCard` to hold the reusable field rendering and POST form. `DigitalProcessCreatePage` now provides the shared Page shell and delegates fields to that card; current create endpoints keep their exact URLs and payloads.
- Added `SubmitLabel` to the page model (default `Create`) to allow the same mutation card to be reused by edit flows without another field renderer.
- Avoided duplicating labels for searchable entity selectors.

Follow-up: entity-specific edit resolvers need to supply prefilled values and update actions to use the new mutation card; this revision does not invent edit endpoints. No runtime build validation is possible without a .NET SDK.
