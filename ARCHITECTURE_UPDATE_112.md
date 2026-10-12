# ARCHITECTURE_UPDATE_112

## Oprava CS0305 a CS0108 po aktualizaci ZIP 110 → 111

**Příčina:** `ZArchitecture_110.zip` obsahuje `src/Core/SecurityLayer/SecuredBaseService.cs` s deklarací `SecuredBaseService<TDBModel,TDTOModel> : ... IBaseService<TDBModel,TDTOModel>`. Soubor byl ve verzi 111 odstraněn ze ZIPu, ale při rozbalení přes existující adresář zůstává fyzicky na disku. SDK standardně kompiluje všechny `*.cs`, takže nalezne starou dvouparametrickou třídu a její pomocné metody (CS0305, CS0108).

## Změny

- `src/Core/SecurityLayer/SecurityLayer.csproj`: `<Compile Remove="SecuredBaseService.cs" />`. Ochrana při aktualizaci do již existujícího adresáře – starý soubor nebude omylem sestaven.
- Nová podporovaná implementace zůstává `src/Core/SecurityLayer/SecuredEntityService.cs` s `SecuredBaseService<TDBModel> : IBaseService<TDBModel>`.
- Žádná změna RBAC, DTO constraintů nebo aplikačních API.

## Důležité po rozbalení

Stále **doporučujeme fyzicky odstranit** starý soubor z pracovní kopie:

```sh
rm -f /workspace/src/Core/SecurityLayer/SecuredBaseService.cs
# z kořene repozitáře:
dotnet clean
dotnet build
```

Pro ověření:

```sh
grep -R 'class SecuredBaseService<TDBModel, TDTOModel>' src --include='*.cs'
```

Výstup tohoto grep příkazu musí být prázdný. `Compile Remove` není zamýšleno jako trvalé uchování legacy třídy; je pojistkou pro aktualizaci ZIPem.

## Ověření

Provedena statická kontrola obsahu archivu a integrity ZIP. `.NET SDK` není v tomto prostředí nainstalováno, proto sestavení ani testy nebyly spuštěny.
