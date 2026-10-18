# ZArchitecture 132 – Standardní typy událostí

- `src/Apps/Events/EventAdminPage.razor`: administrační stránka `/events/EventResponse/admin`, přístup z navigace i seznamu událostí.
- `StandardEventTypes.cs`: přesných 20 položek z `excel.babel.html` včetně historických UUID, českých názvů, zkratek a barev; přidány jednoznačné stabilní kódy.
- `EventTypeImport.cs`: import s kontrolou existence podle UUID **nebo kódu**, bez přepisování existujících dat. Oprávnění `create` nad vybraným RBAC rozsahem; zápis v serializovatelné transakci.
- `POST /events/EventTypeResponse/import-standard` s JSON `{ "rbacObjectId": "..." }` vrací `inserted`, `existing`, `total`.
- Záměrně se neimportují ukázkové instance událostí z HTML, pouze typy.

## Poznámky

Už vytvořené typy se nemění; výsledek zobrazuje počty. Řádek „Nezobrazen / chybí“ na administrační stránce zahrnuje také typy, které nejsou dostupné uživateli podle RBAC. Dvojí současný import může skončit databázovým konfliktem v serializovatelné transakci, ale unikátní omezení zabraňují duplicitám; opakování požadavku je bezpečné.

V tomto prostředí nebyl k dispozici .NET SDK; kompilaci a runtime testy je nutné ověřit v devcontaineru.
