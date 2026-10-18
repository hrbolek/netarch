# Architecture update 64: response representations in OpenAPI

- New shared `ProducesEntity<T>()` extension in Base.Endpoints.
- `ApplyRepresentation<TView,TModel>()`, therefore the existing HTML/JSON `ApplyViewModel` and two-generic `ApplyJsonModel`, registers HTTP 200 `application/json` with DTO schema and `text/html`.
- One-generic `ApplyJsonModel<TModel>()` remains JSON-only and advertises only JSON, avoiding false claims of HTML rendering.
- Added descriptive `WithDescription` to previously undocumented standard list/view maps where applicable. Existing business descriptions retained.
- 404/403/400 are not advertised automatically: concrete routes can add them explicitly where applicable.
- This changes OpenAPI metadata only; HTTP content negotiation remains unchanged.
- The provided archive was not dotnet-built in this environment (SDK unavailable).
