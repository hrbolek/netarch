# ZArchitecture 72.5 — Swagger export after startup

- `Program.cs`: the application is started with `StartAsync()` before `ExportSwaggerJsonAsync()`, then kept alive with `WaitForShutdownAsync()`.
- `SwaggerExportExtensions`: exporting a document with no `paths` now throws an explicit exception instead of overwriting a valid JSON export with an empty document.
- Existing endpoint registrations and response DTOs are unchanged.
- `SWAGGER_EXPORT=false` and `SWAGGER_EXPORT_PATH` retain their meaning.

Test after starting Host: compare `openapi/swagger.json` with `/swagger/v1/swagger.json`; both should have nonempty `paths`. Note that `PreSerializeFilters` (if added later) are applied only by HTTP Swagger middleware, not by a direct provider export.
