# ZArchitecture76 – funkční import a diagnostika

- Opraven zásadní problém: RazorComponentResult je statické HTML, takže Blazor `@onclick` v předchozích verzích nebyl aktivní.
- Import běží přes browser fetch POST `/digital-processes/form-data/import`.
- Chyby HTTP, GraphQL `errors`, chybná konfigurace a síťové chyby se ukazují přímo na formuláři.
- `appsettings.Development.json` volí GraphQL namísto Dummy (URL přizpůsobte skutečnému endpointu).
- Základní `$form.*` lze vyhodnotit z DOM, `$context.*` a `$process.*` nejsou zatím podporovány přes tento demonstrační endpoint.

**Bezpečnost:** Endpoint se registruje pouze v prostředí Development. Je testovací a zatím nemá autorizaci a povolený katalog dotazů; nesmí se zpřístupnit mimo důvěryhodné vývojové prostředí. Před produkcí je nezbytné kontrolovat oprávnění, omezit dotazy a velikosti odpovědí a řešit audit.

.NET SDK není v prostředí dostupné; ZIP prošel kontrolou integrity.
