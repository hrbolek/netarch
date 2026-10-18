# ZArchitecture 116 — kompozice aplikací výhradně v Apps

- Potvrzený výchozí stav uživatelem: 104 SecurityLayer + 4 Host + 45 DigitalProcesses = **153 zelených testů** pro verzi 115.
- Nový `src/Apps/Host/HostDbContext.cs`: Host je kompoziční kořen EF Core pro aplikace a definuje rozšiřovací bod `OnModelCreating`.
- `src/Apps/Host/HostDatabaseRegistration.cs`: po registraci modulů přesměruje `CoreDbContext` a `DigitalProcessesDbContext` na jednu scoped instanci `HostDbContext`.
- `src/Apps/Host/Program.cs`: kompozice volaná jako poslední po `.AddCore().AddDigitalProcesses()`.
- `DigitalProcessesDbContext`: odpečetěn a doplněn chráněný konstruktor s negenerickým `DbContextOptions`; dosavadní veřejný konstruktor i mapování zachovány pro stávající testy.
- Regresní test `T20_AllApplicationLayersShareOneScopedEfContext` ověřuje identitu kontextu na úrovni DI.
- `src/Apps/Host/ADDING_APPLICATIONS.md`: praktický návod na novou doménu `Projects` se dvěma modely bez změny Core.

**Nezměněno:** zdrojové soubory pod `src/Core/`, bezpečnostní filtr, role/membership, DTO kontrakty, endpoint cesty.

**Omezení:** tento krok stále dědí z DigitalProcessesDbContext kvůli zachování původních mapování. Přesto lze připojovat další samostatné aplikace úpravami pouze pod `src/Apps/`. Další případná fáze může přesunout mapování DigitalProcesses na čistou EF konfiguraci skládající se v Host a odstranit dědičnost mezi aplikacemi.

**Ověření:** statická kontrola, XML/archiv; bez .NET SDK nebylo možné build ani testy spustit.
