# ZArchitecture74.1 — FormBackendOperationRequest variables contract

- `FormBackendOperationRequest` declares `JsonObject? Variables` as a proper C#-style record property (third positional constructor argument remains optional).
- `FormVariableResolver` reads `request.Variables`; the preflight evaluation of `$form.*`, `$process.*`, and `$context.*` is unchanged.
- `FormBackendPreparedRequest` uses a separate transport property `variables`, which is intentionally unchanged and serialized in the HTTP GraphQL payload.
- No new DTOs or routes.
- Build not executed because .NET SDK is unavailable in the artifact environment.
