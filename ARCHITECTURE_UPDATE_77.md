# ZArchitecture77 — server-side HTMX preview import

- Test formuláře nyní obsahuje identifikátor serverové preview instance; sdílený HTML formulář odesílá autosave HTMX (`change delay:300ms`).
- `POST /digital-processes/form-preview/values` validuje verzi a ukládá povolená pole do IMemoryCache po dobu 40 minut (sliding expiration).
- `POST /digital-processes/form-preview/actions/{actionId}` načítá konfiguraci akce **z definice formuláře na serveru**, nikoli z browseru. Nejprve uloží hodnoty zaslané s akcí, potom přes `IFormDataBackend` vyhodnotí variables v `PreprocessAsync`, volá serverjsonata a vrátí HTML fragment pro HTMX.
- Importní komponenta již neobsahuje JavaScript `fetch` ani parser proměnných v prohlížeči.
- Zpracované hodnoty jsou v serverové preview instanci; při shodě cesty s názvem uzlu se aktualizují i jeho hodnoty v preview store. HTML fragment zobrazuje importovaný objekt a u textových, číselných, datumových a vícerádkových polí používá HTMX OOB výměnu skutečných inputů. Ostatní typy vstupů zatím nejsou aktualizovány v HTML.
- Omezení: **Preview only**, bez trvalého Submission; IMemoryCache je lokální pro jeden proces; žádný klientský instance GUID není kryptografická autentizace. Endpointy běží jen v Development; produkční workflow potřebuje RBAC na vykonání akce, CSRF ochranu a verzování stavu, až bude napojeno na perzistentní Submission. `context.*` a `process.*` nejsou dostupné, dokud se nepřipojí serverový execution context.
- Cílový `Endpoint` v `src/Host/appsettings.Development.json` musí odpovídat skutečné cestě serverjsonata.
- `dotnet` SDK nebylo v prostředí dostupné; nutno provést `dotnet build src/Host/Host.csproj` v devcontaineru.
