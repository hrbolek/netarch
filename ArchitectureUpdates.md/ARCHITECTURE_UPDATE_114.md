# ARCHITECTURE_UPDATE_114

## Cíl

Oddělit typové DTO schopnosti pro SQL read projekci, mapování výsledku, vytváření a aktualizaci tak, aby `SecuredBaseService<TDBModel>` nebyla vázána na univerzální DTO vyžadující všechny operace.

## Změny

- Nový `IEntityProjection<TDBModel,TDTO>` s `static abstract Expression<Func<TDBModel,TDTO>> Projection`. Je určen pro SQL `Query`, `GetById`, `Where`, `VectorAsync` a stránkování.
- Nový `IEntityMapper<TDBModel,TDTO>` s `static abstract TDTO FromModel(TDBModel model)` pro výsledky aktualizací a jiné převody materializovaných modelů.
- Existující vstupní kontrakty `ICreateDTOModel<TDBModel>` (`ToModel()`) a `IUpdateDTOModel<TDBModel>` (`ApplyTo(...)`, `id`) zůstaly nezávislé.
- Odstraněno staré kombinované `IEntityDTOModel<TEntity,TSelf>` (soubor `IEntityDTOModel.Generic.cs`). Negenerecké `IEntityDTOModel` s identitou (`id`, `visual_label`) zůstává pro UI a odkazy.
- 21 Response DTO v Core a DigitalProcesses implementuje projekční a mapovací kontrakt samostatně, bez povinného `ICreateDTOModel`.
- `ISecuredEntityService`, `IBaseService`, `SecuredBaseService` a `BaseResolvers` používají capability-specific `where` constraints. Update output vyžaduje mapper, při HTTP update musí output navíc podporovat SQL projekci pro načtení současného stavu.
- Zachována jedno-generická `SecuredBaseService<TDBModel>`.

## Bezpečnostní záměr

`Query<TDTO>` vždy začíná RBAC-filtered `SecuredModels(...)` a až pak aplikuje `Select(TDTO.Projection)`. Update načte autorizovanou entitu a používá `Security.StageUpdateAsync` uvnitř transakce. Specifické override aktualizace skupin, rolí a členství nebyly odstraněny.

**Create není nově povoleno jako generická metoda `SecuredBaseService`.** Specializované Create metody mají vedle autentizace a autorizace i důležité doménové invarianty a synchronizaci EffectiveAccess. Existující `ICreateDTOModel<TDBModel>` může být používán ve specializovaných Create metodách, ale sám není důvodem poskytnout veřejný generický Create endpoint.

## Kontrola a omezení

- Statická kontrola: v C# zdrojích nezůstává `IEntityDTOModel<TEntity,TSelf>`; 21 Response DTO implementuje oddělené schopnosti.
- Archiv byl ověřen na integritu ZIP.
- **Nebyl proveden `dotnet build` ani PostgreSQL testy: v pracovním prostředí není .NET SDK.** Neprohlašujeme sestavení za úspěšné.

## Doporučené ověření

```bash
dotnet clean
dotnet build
```

Ověřte zejména `UpdateAsync<TUpdateRequest,TDTO>`, HTTP update, specifická RBAC filtrování rolí a členství, CUD transakce a synchronizaci EffectiveAccess.
