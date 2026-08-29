# WebAppArchitecture layer templates

Každá šablona generuje samostatnou DLL vrstvu pro jednu entitu. Registrace zůstává explicitní; žádný assembly scanning není potřeba.

## Instalace šablon

Z rootu repozitáře:

```bash
dotnet new install ./templates/EntityModelTemplate
dotnet new install ./templates/EntityServiceTemplate
dotnet new install ./templates/EntityResolversTemplate
dotnet new install ./templates/EntityUITemplate
```

## Vygenerování nové entity

Příklad `Document`:

```bash
dotnet new entity-model     -n Document -o src/Document.Models     --rootNamespace MyApp
dotnet new entity-service   -n Document -o src/Document.Services   --rootNamespace MyApp
dotnet new entity-ui        -n Document -o src/Document.UI         --rootNamespace MyApp --title "Dokument"
dotnet new entity-resolvers -n Document -o src/Document.Resolvers --rootNamespace MyApp --route document
```

Přidání do solution je záměrně explicitní:

```bash
dotnet sln SignFakeArchitecture.sln add src/Document.Models/Document.Models.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.Services/Document.Services.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.UI/Document.UI.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.Resolvers/Document.Resolvers.csproj
```

## Závislosti

```text
Base
  ↑
Models
  ↑       ↑
Services  UI
   \      /
    Resolvers
       ↑
      Host
```

Response DTO je v `Models`, nikoli v `Resolvers`, aby UI a Resolvers nevytvářely kruhovou závislost.

## Host

Host explicitně zapne služby a endpointy:

```csharp
using MyApp.Document.Services;
using MyApp.Document.Resolvers;

builder.Services.AddRazorComponents();
builder.Services.AddDocumentServices();

var app = builder.Build();

app.MapDocumentEndpoints();
```

Resolver template vytvoří například:

```csharp
var group = endpoints.MapGroup("/document");

group.MapGet("/{id:guid}", DocumentResolvers.Read)
    .ApplyViewModel<DocumentViewPage, DocumentResponse>();
```

## Hasura-like filtrovaný vektor

Base vrstva obsahuje `HasuraFilterExpression`, který převádí explicitní dictionary filtr na `Expression<Func<TModel,bool>>`. Resolver template publikuje `POST /<route>/vector`; resolver provede překlad a service/store vrstva pracuje už pouze s typovaným predikátem.

```json
{
  "where": {
    "Name": { "_ilike": "%demo%" },
    "_or": [
      { "Name": { "_eq": "Alpha" } },
      { "Name": { "_eq": "Beta" } }
    ]
  }
}
```

Toto rozdělení záměrně drží Hasura-like transportní syntaxi v resolver vrstvě a `Expression<Func<...>>` jako kontrakt service/store vrstvy.
