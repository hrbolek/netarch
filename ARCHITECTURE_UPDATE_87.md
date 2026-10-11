# ARCHITECTURE_UPDATE_87

## AI návrhář formuláře – lehké potvrzovací UI

- Návrhář rozpracované FormVersion nyní obsahuje textový pokyn, tlačítko **Navrhnout změny**, JSON výsledku a přehled nových cest FormNode oproti aktuálnímu formuláři.
- UI volá `POST /digital-processes/ai/form-design` (kontrakt verze 86, FastAPI + PydanticAI).
- Uživatel může JSON zkontrolovat/upravit a explicitně potvrdit přes **Potvrdit a uložit návrh**.
- Nový `POST /digital-processes/ai/form-design/approve` provádí serverové ověření RBAC (`Update` na FormVersion a uzlech), režimu draft, povolených rendererů, platných technických klíčů, hloubky, počtu uzlů a převádí strom na `BulkFormTreeRequest`.
- Atomický zápis používá existující `IBulkTreeService`. Stávající uzly se párují podle technických klíčů a rodičů. **Vynechané uzly se nemažou.** Přesun uzlu a změna jeho rendereru nejsou dovoleny. Po uložení se obnoví návrhář.
- Autorizovaný návrh se ukládá pouze do existující *draft* FormVersion. Publikovanou verzi nelze upravit a novou FormVersion zatím UI samo nevytváří.

## Identita agenta

`x-ms-client-principal` je Base64-kódovaný JSON reprezentující claims (obvykle `claims`, `auth_typ`, `name_typ`, `role_typ`), **není to podepsaný JWT**. V prostředí EasyAuth se mu důvěřuje pouze jako hlavičce vložené důvěryhodnou autentizační bránou. Agentovi jej není bezpečné poslat jako univerzální přístupový token, pokud API současně nekontroluje, že jej může vložit pouze důvěryhodná proxy. Pro agenta vyžadujícího zpětné volání API je doporučen krátkodobý delegovaný JWT s audience a rozsahem, nebo servisní důvěryhodná proxy, která aplikuje efektivní uživatelskou identitu. V této verzi se principal agentovi neposílá.

## Konfigurace

Nastav `AiAgents:formdesign_aiagent:enabled=true` a `endpoint` na důvěryhodné FastAPI. UI a server používají stejný endpoint jako verze 86.

## Ověření

Kontrola ZIP a syntaktická kontrola vloženého JavaScriptu. .NET sestavení a integrační testy nebyly provedeny (SDK zde není dostupné). Databázová migrace není potřeba.
