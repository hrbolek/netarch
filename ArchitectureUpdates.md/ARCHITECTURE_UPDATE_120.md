# ZArchitecture 120 — sjednocení prezentačních prvků CoreUI

## Rozsah

Tato verze představuje **inkrementální** stabilizaci CoreUI na základě ZArchitecture 119. Záměrně nemění RBAC, EF kontexty, DI registrace, HTTP routy ani stávající doménové stránky. Staré `PageView`, `PageEdit`, `PageBase` a doménové `*LargeCard` zůstávají pro kompatibilitu.

### Nové generické stavební prvky

- `TypedLargeCard<TContext,TLeftColumnContent>`: generický dvousloupcový kontextový layout; předává `Model` komponentě levého sloupce; hlavní obsah přijímá jako `RenderFragment<TContext>`. Využívá dosavadní `LargeCard<TModel>`.
- `TypedEntityPage<TContext,TLeftColumnContent>`: kompozice dokumentu `Page` a kontextového layoutu; nepředpokládá zvláštní doménovou `*Page`.
- `View<TModel,TComponent>`: zobrazení jednoho modelu prostřednictvím konkrétní prezentační komponenty.
- `ViewVector<TModel,TComponent>`: kolekce modelů renderovaná stejnou prezentační komponentou, bez další doménové stránky.

**Proč zatím `TypedLargeCard` místo přetížení `LargeCard<TContext,TLeftColumnContent>`:** Razor komponenta `LargeCard.razor` už existuje s jedním typovým argumentem a používají ji desítky míst. Samostatný název dovoluje ověřit generický vzor bez hromadných změn a kolize komponent podle arity. Zůstává kandidátem pro sjednocení po sestavení a ověření.

### Opravy dosavadních komponent

- `LargeCard<TModel>` už implicitně nevkládá `MediumCard` do středního sloupce, pokud chybí `ChildContent`. Levou kartu výslovně přidává `SideContent`, resp. nový typovaný layout.
- `VectorContent`, `CardVectorContent`, `CompactCardVectorContent` a `TableVectorContent` používají `IEntityRouteRegistry` pro odkazy `view` a `actions` místo ručně sestavovaných URL. `Model.App` a `Model.EntityType` zůstávají explicitními overrides, pokud je poskytuje view model.
- `PageEdit` už ve výchozím případě nevykresluje nefunkční placeholder `EditableCard`; upozorní na nutnost dodat skutečný editor.
- `templates/EntityUITemplate/COREUI_COMPOSITION.md` vysvětluje preferovanou skladbu.

### Záměrně nedokončené

- Automatické generování komponent CRUD (`Create`, `Update`, `Delete`) zatím **nezavádíme**: jejich skutečnou funkcionalitu je třeba opřít o existující formuláře, validační a autorizační pravidla. `View`, `ViewVector` jsou pouze vykreslovací prostředky.
- Neodstraňujeme stávající doménové `.razor` komponenty ani nekontrolovaně nepřesouváme CoreUI projekty.
- UI prefixy zapsané natvrdo v existujících doménových komponentách nejsou v této verzi plošně přepracovány.
- Rozhraní `IComponent` omezuje typ komponenty, ale nezaručuje statickou přítomnost parametru `Model`. Tento runtime kontrakt je dokumentován.

## Ověření

ZIP byl zkontrolován na integritu a strukturální úplnost nových komponent. **Dotnet SDK v prostředí není**, proto sestavení a testy nelze potvrdit. Před dalšími změnami spustit:

```bash
dotnet test SignFakeArchitecture.sln
```

Následně ověřit Razor rendering nové `TypedEntityPage` s `UserResponse` + `UserMediumCard`, `ViewVector` s `RoleMediumCard`, odkazy pro jiné prefixy a že stávající detailové a editační stránky zůstaly funkční.
