# Architecture Update 48

Fixes compile errors introduced by the Core.Hosting modularization.

- `CoreModule.cs` now aliases the concrete `DBLayer` and `SecurityLayer` types to avoid namespace/type name collisions.
- Added `Microsoft.AspNetCore.Http` so endpoint metadata extensions such as `WithTags` are available.
- No runtime or architectural behavior was changed.
