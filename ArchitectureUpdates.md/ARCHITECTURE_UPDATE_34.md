# Architecture Update 34

## DigitalProcesses resolver structure

`DigitalProcesses.Resolvers/Resolvers` now mirrors the Services structure:

- `Forms/`
- `Workflow/`
- `Process/`
- `Runtime/`
- `Common/`

Each entity has its own resolver class and source file. Endpoint mapping uses named resolver methods only; anonymous route handlers are not used.

## Endpoint documentation

All Minimal API endpoint mappings under `src` now include `.WithDescription(...)` metadata so Swagger/OpenAPI exposes a description for every mapped route.

Host diagnostics and legacy RBAC mappings were also converted to named handler methods.

## Git ignore

Added:

```text
**.zip
```
