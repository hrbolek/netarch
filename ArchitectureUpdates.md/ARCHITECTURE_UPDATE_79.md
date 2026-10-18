# ARCHITECTURE_UPDATE_79

## Zaměření

DigitalProcesses: správa revizí dokumentových RTF šablon, vazby na stabilní FormNode IDs a sjednocení navigace „Vytvořit“.

## Změny

1. Odstraněno samostatné tlačítko **Create** ze společné stránky `DigitalProcessListPage.razor`. Obecné vytváření je nadále dostupné přes navigační menu; kontextové akce u konkrétní entity zůstávají.
2. `DocumentTemplateRevisionModel` (`dp_document_template_revisions`) ukládá neměnný obsah RTF a číslo revize. Unikátní klíč: (template_id, revision).
3. `TemplateFieldBindingModel` (`dp_template_field_bindings`) propojuje token šablony se stabilním `FormNode.id`; unikátní klíč (revision_id, placeholder), cizí klíč na FormNode.
4. `RtfTemplateStore.Save` při založení šablony současně založí revizi 1. Původní endpointy a stávající generování zůstávají použitelné.
5. Nové API `TemplateManagementEndpoints`:
   - `GET /digital-processes/document-templates/{id}/revisions`
   - `GET /digital-processes/document-templates/revisions/{revisionId}/content`
   - `POST /digital-processes/document-templates/{id}/revisions` s JSON `RtfBase64` a `Bindings` (`Placeholder`, `FormNodeId`)
   - `GET /digital-processes/document-templates/form-schema/{formVersionId}`
6. Vytvoření revize ověřuje oprávnění Update nad FormVersion, RTF a velikost (max. 5 MB), existenci a čitelnost všech svázaných FormNodes a jednotnost placeholderů. Zápis probíhá v transakci. Původní renderovací endpoint bere nejnovější obsah; historie revizí je zachována.
7. Návrhář formuláře nyní ukazuje vstup do správy šablon, dostupné schéma a samostatný deaktivovaný AI placeholder pro návrh šablony ze vzoru.

## Omezení této verze

- AI/MCP agent **není** připojen; žádný vzorový dokument se na server neodesílá. Placeholder pouze vyznačuje plánované místo.
- Vizualizace správy šablon je zatím odkazy na API, nikoli plnohodnotný interaktivní editor RTF.
- Dokumentová akce je nadále ve stavu předchozí etapy: plná produkční cesta Submission → dokument → multipart → JSONata → import ještě není hotová.
- Nové tabulky vyžadují aplikování změn schématu v již existující `postgres_development`. `EnsureCreated` **neaktualizuje** existující databázi. Pro existující PostgreSQL je přiložen soubor `migrations/ARCHITECTURE_79_postgresql.sql` obsahující idempotentní DDL a backfill revize 1; před jeho použitím zálohujte databázi. Databázi s daty nemažte.
- Případné starší šablony vytvořené před zavedením revizí mají zachovaný obsah v `dp_document_templates`, ale backfill lze provést přiloženou jednorázovou SQL migrací.
- V této infrastruktuře není .NET SDK. Kompilace ani integrační testy verze 79 nebyly spuštěny. Poslední ověřená výchozí sestava měla 39/39 úspěšných DigitalProcesses testů, nikoli tato změna.

## Příští etapa

Vytvořit uživatelskou stránku se seznamem šablon a revizí, uploadem a náhledem, naplnit historické revize starších šablon, připravit EF migration a dokončit produkční dokumentovou akci. Následně připojit AI/MCP agenta se schvalováním návrhů.
