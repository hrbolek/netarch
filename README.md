# WebAppArchitecture modular architecture

Výchozí bod je rozdělen na společnou `Base.dll` a čtyři `dotnet new` šablony, které generují samostatné vrstvy/DLL pro entitu.

```text
SignFakeArchitecture.sln
src/
  Base/
    Base.csproj

templates/
  EntityModelTemplate/
  EntityServiceTemplate/
  EntityResolversTemplate/
  EntityUITemplate/
  README.md
```

`Base.csproj` obsahuje:

- `IEntityModel`, `IBaseStore<T>`, `InMemoryStore<T>`,
- `IBaseService<T>` a `BaseService<T,TStore>`,
- `ApplyViewModel`, `ApplyJsonModel`, `ApiResults`,
- explicitní `WithEntra` / `WithoutEntra`,
- obecné Razor komponenty (`CardCapsule`, `MediumCard`, `LargeCard`, `EntityLink`, `Attribute`),
- obecné stránky (`Page`, `PageBase`, `PageView`, `PageEdit`, `PageViewList`),
- HTMX `LazyContent`.

Endpoint registrace v generované resolver vrstvě je explicitní a používá `MapGroup(...)`.

Viz `templates/README.md` pro instalaci a scaffoldování nové entity.

## Filtrované vektory entit

Service/store kontrakt podporuje `VectorAsync(Expression<Func<TModel,bool>> predicate)`. Resolver template překládá Hasura-like JSON dictionary přes `HasuraFilterExpression` a explicitně publikuje `POST /<entity-route>/vector`. Transportní filtr tedy neprosakuje do service vrstvy a stejný predikát lze použít nad in-memory i databázovým store.
