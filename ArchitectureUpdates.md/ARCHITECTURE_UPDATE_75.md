# ZArchitecture75 – konfigurovatelný import jako FormNode

- Nový systémový FormNodeType `Import dat (GraphQL)` (`renderer_key=action.import`) je automaticky seedován při spuštění i do existující databáze (stálé UUID).
- Form designer: Add node / Add child -> Type `Import dat (GraphQL)`, nastavte key např. `loadWhoami`, label a `Configuration JSON`.
- Výchozí Configuration JSON: `{"label":"Import dat","query":"query { whoami { hostname } }","variables":{}}`.
- Po přidání editace konfigurace přes Structure -> Edit properties -> Configuration JSON.
- V Test režimu se tlačítko vykresluje v místě uzlu. Import používá existující `IFormDataBackend`, preflight a vykreslení odpovědi `data`.
- Odstraněn vždy viditelný demonstrační importní panel; akce vzniká pouze z definice formuláře.
- Zatím prototyp: načtené hodnoty jsou uvnitř importního uzlu, nejsou synchronizovány s ostatními FormNode ani ukládány do Submission.
- `appsettings.json` / env konfigurace backendu zůstává beze změny.
- ZIP ověřen; .NET SDK není v tomto prostředí dostupné, build neproveden.
