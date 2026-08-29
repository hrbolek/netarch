# EntityServiceTemplate

Generuje samostatný `EntityName.Services.csproj`, service interface/implementaci a `AddEntityNameServices()`.

```bash
dotnet new entity-service -n Document -o src/Document.Services --rootNamespace MyApp
```

Výchozí project references očekávají `../Base` a `../Document.Models`.

## Vektor entit

`IBaseService<TModel>` obsahuje vedle `GetAllAsync()` také:

```csharp
Task<IReadOnlyCollection<TModel>> VectorAsync(
    Expression<Func<TModel, bool>> predicate,
    CancellationToken cancellationToken = default);
```

Service nepřekládá transportní filtr. Dostává již typovaný predikát a deleguje jej do store vrstvy. In-memory store používá `predicate.Compile()`, databázový store může stejný expression předat do `IQueryable.Where(predicate)`.
