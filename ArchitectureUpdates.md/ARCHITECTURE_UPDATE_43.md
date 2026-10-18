# Architecture Update 43

Fixes the build regression introduced by Update 42.

## Fix

`src/Core/Base/Resolvers/VectorQueryParser.cs` now explicitly imports:

```csharp
using Microsoft.AspNetCore.Http;
```

`Base.csproj` already references `Microsoft.AspNetCore.App`; therefore no project/package change is required.

No runtime or architectural behavior was changed.
