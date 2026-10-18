# ARCHITECTURE_UPDATE_101 — Připnuté odkazy a sticky navigace

- `RecentEntityHistory.razor`: dvě nezávislé skupiny v jednom řádku — až 5 připnutých entit a 5 nedávných nepřipnutých entit.
- Připnutí přes ikonu vedle odkazu, odepnutí jej vrátí mezi poslední navštívené. Při plných pěti připnutých se další připnutí zakáže; nic se automaticky neodepíná.
- Unikátnost dle `app/entity/id`; `action` se ignoruje pro identitu, ale poslední navštívená URL se zachovává. Připnutá entita se nesmí zároveň objevit v historii.
- `localStorage`: zachován původní klíč `zarchitecture.recentEntities.v1`, nový `zarchitecture.pinnedEntities.v1`; stávající historie se zachovává.
- `Page.razor`: Bootstrap `sticky-top` společně kolem horního menu i historie, aby obojí drželo při scrollování.
- Vodorovná historie má overflow-scroll; dlouhé labely se zkracují pomocí ellipsis.
- Nedochází ke změnám DB, API ani RBAC.

## Ověření

Proveďte `dotnet build SignFakeArchitecture.sln` a otestujte ručně v prohlížeči: připnutí pěti položek, zákaz šesté, odepnutí, obnovení stránky, přechody `view/edit`, přesunutí navštívené nepřipnuté entity na konec a sticky menu při scrollu. Zde není .NET SDK.
