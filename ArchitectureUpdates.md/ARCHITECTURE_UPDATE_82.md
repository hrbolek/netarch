# ARCHITECTURE_UPDATE_82 — výpočty formuláře a display.url

## Implementace

- Nový read-only FormNode type `display.url` (`7d80c90b-f7b3-45f2-a5c8-7e54d23d1011`), seeding při startu aplikace, registrace Razor rendereru.
- Společné `IFormCalculationService` / `FormCalculationService` nad existujícím `IFormDataBackend` (GraphQL/JSONata). Vyhodnocení nepřidává druhou síťovou cestu a respektuje stávající backendovou autentizaci.
- Konfigurace `calculation.query`, `calculation.variables` a volitelná `calculation.path` pro extrakci části JSON výsledku. Alternativně `dataSource` kvůli kompatibilitě se `input.select`.
- `input.select` používá stejnou službu, statické `options` zůstávají beze změny.
- Renderer URL akceptuje pouze absolutní `http` a `https`, nastavuje `rel="noopener noreferrer"`, nezobrazuje technickou výjimku uživateli.
- Žádná SQL migrace: známé FormNodeType jsou seedovány při startu.

## Příklad configuration_json

```json
{
  "calculation": {
    "query": "query ($id: ID!) { document(id: $id) { url label } }",
    "variables": { "id": "example" },
    "path": "data.document"
  }
}
```

Backend musí vrátit např. `{ "data": { "document": { "url": "https://example.org/doc/1", "label": "Dokument" } } }`.

## Hranice řešení

Aktuální výpočty se spouštějí v okamžiku vykreslení komponenty, nikoli při každém stisku klávesy nebo změně jiné komponenty. Pro live-reactivity a produkční Submission endpointy je potřeba navazující krok: snapshot všech dostupných hodnot, dependency tracking, cache a synchronizace událostí, server-side autorizace. `action.import` stále používá svoji transportní a mapping logiku; sjednocena je výpočetní cesta pro select + url, nikoli zatím importní mutace.

Stávající `IFormDataBackend` umožňuje bezpečnostní politiku v jedné integrační vrstvě; nelze zadávat libovolnou URL do form config a neprovádí se automatické uložení návratové hodnoty.

## Testování

Statická kontrola zdrojového kódu a ZIP. .NET SDK v sestavovacím prostředí není dostupný; build a testy zatím nejsou potvrzené.

```sh
dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug
```
