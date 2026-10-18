# ARCHITECTURE_UPDATE_134

## Oprava PostgreSQL DateTimeOffset / timestamptz

- `src/Apps/Events/EventsJsonImport.cs`: data s explicitním `Z` nebo časovým offsetem i lokální časy `Europe/Prague` se převádějí do UTC před uložením. Při změně časového pásma se zachovává skutečný okamžik.
- `src/Apps/Events/EventService.cs`: normalizace do UTC také při vytvoření/editaci události, uložení akademického plánu a nastavení oken registrační politiky.
- Nemění se databázové schéma ani obsah jiných modulů.
- Import zůstává transakční. Pokud předchozí neúspěšný import skončil chybou uvnitř transakce, měla by se transakce vrátit zpět; při opakování importu se existující ID přeskakují.

## Ověření

Kontrolováno staticky ze zdrojového kódu. Prostředí nemá .NET SDK; kompilaci ani skutečné připojení k PostgreSQL nelze potvrdit.

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug
```
