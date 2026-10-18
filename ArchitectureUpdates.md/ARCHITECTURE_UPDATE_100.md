# ZArchitecture update 100 — popisky historie navigace

- Historie zůstává jednořádková a ukládaná v `localStorage`, s maximem pěti entit a unikátním klíčem app/entity/id.
- `LargeCard<TModel>` publikuje bezpečně HTML-enkódovaný popisek z typovaného modelu (`data-recent-entity-label`).
- `RecentEntityLabel.For` vybírá `email`, potom `name`, následně `visual_label`; neinformativní GUID ignoruje a nakonec použije osm znaků ID.
- `RecentEntityHistory` používá tento popisek jako `EntityType · hodnota`, obnoví i starší uložené popisky při další návštěvě entity, a zachová cílovou URL včetně akce.
- Pro stránky bez LargeCard zůstává fallback na nadpis. Úplné pokrytí takových stránek vyžaduje doplnění datových metadat ve specifických Razor komponentách.
- Bez zásahu do databáze, RBAC a stávajících endpointů.

Ověření: syntaktická kontrola upravovaných souborů a ZIP integrity; build a browser test neprovedeny (dotnet SDK není v prostředí).
