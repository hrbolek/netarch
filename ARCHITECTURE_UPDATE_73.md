# ZArchitecture73 — první integrační scénář serverjsonata

- `DigitalProcesses:DataBackend:Provider=GraphQL` aktivuje reálný `GraphqlLikeFormDataBackend` i v Development; `Dummy` je implicitní volba.
- `DigitalProcesses:DataBackend:Endpoint` obsahuje **plnou URL GraphQL HTTP endpointu** (nikoliv pouze host/port). Např. `http://serverjsonata:3000/<skutečná-cesta>`; cesta musí odpovídat skutečně fungujícímu endpointu.
- Komponenta `ExternalDataImportSection` představuje první ověřitelný scénář v testovacím zobrazení designeru a ve FormTreePage. Tlačítko „Import dat“ pošle `query { whoami { hostname } }` a načte `data.whoami.hostname` do editovatelného textového pole v sekci `whoami`.
- Komponenta využívá výhradně `IFormDataBackend.ExecuteAsync`; nepřidává zvláštní API endpoint pro whoami.
- Importní panel je **zatím demonstrační**: nemění persistentní `FormNode` definici ani `Submission` a hodnotu neukládá do DB. Následující krok je obecný binding dat podle `FormNode.configuration_json`.

Docker Compose / devcontainer prostředí:

```yaml
environment:
  DigitalProcesses__DataBackend__Provider: GraphQL
  DigitalProcesses__DataBackend__Endpoint: http://serverjsonata:3000/REPLACE_WITH_ACTUAL_GRAPHQL_PATH
```

Je potřeba, aby DNS `serverjsonata` bylo dostupné ze sítě aplikace. `dotnet build src/Host/Host.csproj` je nutné ověřit lokálně.
