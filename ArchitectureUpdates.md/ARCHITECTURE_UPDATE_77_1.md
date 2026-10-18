# ZArchitecture77.1

- Fixed CS1061 for `IHostEnvironment.IsDevelopment()` in `DigitalProcessesModule.cs` by importing `Microsoft.Extensions.Hosting`.
- Preview endpoints remain mapped only in Development.
- No change to GraphQL request processing.

Build with `dotnet build src/Host/Host.csproj`.
