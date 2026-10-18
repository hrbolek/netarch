# Architecture update 72 — OpenAPI export and declaration separation

## OpenAPI export

`src/Core/Hosting/SwaggerExportExtensions.cs` adds `ExportSwaggerJsonAsync` using Swashbuckle's registered `ISwaggerProvider` and OpenAPI 3.0 serializer. The Host calls it **after** `MapCore()` and `MapDigitalProcesses()`, immediately before `RunAsync()`.

- Default: export to `openapi/swagger.json` relative to `ContentRootPath` during every startup.
- `SWAGGER_EXPORT=false` disables exporting.
- `SWAGGER_EXPORT_PATH=/some/path/swagger.json` changes destination, useful for mounted container volumes.
- `app.ExportSwaggerJsonAsync(path, documentName)` can also be invoked programmatically.
- Export does not include response-specific `UseSwagger().PreSerializeFilters`; it uses the same underlying generator.

## Source organization

Previously combined top-level C# declarations have been separated into named files across `src/Core` and `src/Apps/DigitalProcesses`: interfaces, services, domain models, DTOs, endpoint mapping extensions, and resolver classes. Namespaces and the public signatures remain unchanged. The generic and non-generic forms of `IEntityDTOModel` intentionally share `IEntityDTOModel.cs`.

No routes or response DTO contracts were intentionally changed.

## Verification limits

The current editing environment does not have `dotnet`, so compile/runtime checks and Swagger generation must be confirmed in the deployment/devcontainer environment:

```sh
dotnet build src/Host/Host.csproj
SWAGGER_EXPORT_PATH=/tmp/swagger.json dotnet run --project src/Host/Host.csproj
```

Note: because code was split programmatically, a build is necessary before deployment.
