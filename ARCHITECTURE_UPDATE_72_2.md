# Architecture update 72.2 — fix generic DTO interface

Restores `IEntityDTOModel<TEntity, TSelf>` that was accidentally dropped when `EntityDBModel.cs` was split in update 72.

* The interface now has its own file, `src/Core/Base/Data/IEntityDTOModel.Generic.cs`.
* Its inheritance, constraints, `FromModel` and `Projection` declarations exactly match version 71.
* Existing non-generic `IEntityDTOModel` remains untouched.
* No changes to endpoint contracts or service implementations.

Rebuild with `dotnet clean src/Host/Host.csproj && dotnet build src/Host/Host.csproj`. Actual compilation was not performed in the artifact creation environment (SDK unavailable).
