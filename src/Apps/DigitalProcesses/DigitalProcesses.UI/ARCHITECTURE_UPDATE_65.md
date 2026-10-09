# Update 65 — sjednocení vizuálních reprezentací

Porovnání s Group.UI: GroupViewListPage používá PageViewList a GroupMediumCard; GroupViewPage používá PageView a GroupLargeCard; LargeCard má MediumCard, EntityLinks a EntityPermissions vlevo a specifický ChildContent uprostřed.

DigitalProcesses: list používá PageViewList pro zobrazení entit; view používá PageView s LargeCard; LargeCard doplňuje odkazy a RBAC přehled; MediumCard uvádí základní atributy. Detail neuvádí jen JSON: atributy jsou čitelné a původní JSON je skrytý pod diagnostics. Doménové editory zůstávají zachovány. Bez změn URI, resolverů a API kontraktů.

Poznámka: generické PageView s TComponent vyžaduje ověření kompilace na cílové .NET SDK.
