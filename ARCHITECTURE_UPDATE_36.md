# Architecture Update 36

Fixes a namespace regression introduced when DigitalProcesses resolvers were split into individual files.

## EntityLinkResponse

`EntityLinkResponse` is declared in `WebAppArchitecture.Base.Data`, while the resolver files already import `WebAppArchitecture.Base.UI.Components` for `EntityLinkFragment`.

A project-level global using was added:

```csharp
global using WebAppArchitecture.Base.Data;
```

at:

```text
src/Apps/DigitalProcesses/DigitalProcesses.Resolvers/GlobalUsings.cs
```

This makes `EntityLinkResponse` available consistently to all DigitalProcesses resolver classes without repeating the same using directive in every resolver file.

No endpoint contracts or runtime behavior were changed.
