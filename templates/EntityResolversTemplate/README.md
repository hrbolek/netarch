# EntityResolversTemplate

Generuje samostatný `EntityName.Resolvers.csproj`, static resolvers a explicitní `MapEntityNameEndpoints()`.
Používá `MapGroup(...)`, `ApplyViewModel` / `ApplyJsonModel` a generuje také endpoint pro filtrovaný vektor entit.

```bash
dotnet new entity-resolvers -n Document -o src/Document.Resolvers --rootNamespace WebAppArchitecture.Dokument --route document
```

Výchozí project references očekávají `../Base`, `../Document.Models`, `../Document.Services` a `../Document.UI`.

## Filtrovaný vektor

Template generuje:

```csharp
POST /document/vector
```

Resolver přijme JSON objekt s `Where` dictionary, přeloží jej přes
`HasuraFilterExpression.Build<DocumentModel>(...)` na:

```csharp
Expression<Func<DocumentModel, bool>>
```

a předá jej do:

```csharp
service.VectorAsync(predicate, cancellationToken)
```

Příklad requestu:

```json
{
  "where": {
    "Name": { "_ilike": "%demo%" },
    "_and": [
      { "id": { "_neq": "00000000-0000-0000-0000-000000000000" } }
    ]
  }
}
```

Podporované operátory:

```text
_and _or _not
_eq _neq _gt _gte _lt _lte
_in _nin
_like _ilike
_is_null
```
