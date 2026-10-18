# Architecture Update 49

Fixes ASP.NET Core endpoint extension resolution in `DigitalProcesses.Module`.

Added:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
```

`MapGet` on `IEndpointRouteBuilder` is provided by the endpoint-routing extensions in `Microsoft.AspNetCore.Builder`; `WithTags`/related endpoint metadata extensions require the ASP.NET Core HTTP namespace in this project setup. No runtime or architectural behavior changed.
