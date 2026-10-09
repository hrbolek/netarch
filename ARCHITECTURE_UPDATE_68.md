# Architecture update 68 — OpenAPI content negotiation

## Cause
`FormDefinitionResponse/list` already uses `ApplyViewModel<DigitalProcessListPage<FormDefinitionResponse>, FormDefinitionResponse[]>()`, which calls `ApplyRepresentation` and then `ProducesEntity<TModel>()`. Previous `ProducesEntity` registered two separate 200 response metadata entries. OpenAPI generators can expose just the later media type, making Swagger UI omit one of the choices.

## Change
`ViewModelEndpointExtensions.ProducesEntity<T>` now registers **one** 200 response metadata entry with both `application/json` (typed as `T`) and `text/html`. This applies centrally to `ApplyViewModel<TView,TModel>`, `ApplyJsonModel<TView,TModel>` and direct `ApplyRepresentation<TView,TModel>`. `ApplyJsonModel<TModel>` remains JSON-only by design.

## Verification in running Host
1. `dotnet run --project src/Host/Host.csproj`
2. Inspect `/swagger/v1/swagger.json` (or the configured Swagger document URL).
3. Find `paths["/digital-processes/FormDefinitionResponse/list"].get.responses["200"].content`: it should contain `application/json` and `text/html`.
4. Test the endpoint twice, once with `Accept: application/json` and once with `Accept: text/html`, to check the actual representation.

No SDK was available in the preparation environment, so compile/runtime OpenAPI generation remains to be verified.
