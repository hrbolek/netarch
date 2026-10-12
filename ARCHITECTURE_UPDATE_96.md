# ZArchitecture 96 – ComputedUrlRenderer

## Zjištění

`ComputedUrlRenderer.razor` dříve vždy sestavoval kompletní `FormDataContext`, a to i pro absolutní konstantní URL bez zástupných výrazů. Chyba v některém nesouvisejícím uzlu formuláře (např. nevalidní klíč či duplicitní název) mohla zablokovat i obyčejný odkaz.

Konfigurace komponenty musí být JSON objekt se členem `template`, např.:

```json
{"template":"https://example.org/path","label":"Otevřít","target":"_blank"}
```

Pouhé uložení URL jako JSON stringu nebo do `value` / `url` se ve verzi 95 nevyhodnocovalo jako konfigurace odkazu. Pro parametrizovanou URL: `{"template":"https://example.org/users/{$.form.section.user_id}"}`.

## Změny

- Konstantní URL se vyhodnocuje bez sestavování kontextu ostatních polí formuláře; datový kontext se načítá pouze při použití `{$...}`.
- Placeholder JSON konfigurace v návrháři se nyní pro typ `display.url` skutečně týká odkazů, nikoli importu dat.
- Přidány testy URL šablon: konstanta, substituce a bezpečnost schématu.

## Omezení

Tato oprava neřeší živou aktualizaci odkazu při změnách vstupních polí v browseru, protože komponenta se renderuje na serveru. Testy a build nebylo možné spustit bez .NET SDK. U konkrétní nefunkční konfigurace je třeba ověřit uloženou hodnotu `configuration_json` a HTML atribut `href`.
