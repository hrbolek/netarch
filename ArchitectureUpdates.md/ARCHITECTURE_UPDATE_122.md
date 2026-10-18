# ARCHITECTURE_UPDATE_122

## Oprava CS1061 ApplyViewModel v Apps/Host

Doplněno `using WebAppArchitecture.Base.Endpoints;` do `src/Apps/Host/Program.cs`. Metoda `ApplyViewModel<TView,TModel>()` je definována v `Core/Base/Resolvers/ViewModelEndpointExtensions.cs` v tomto namespace. Žádné změny routingu, chování endpointů ani projektových referencí.

Verze vychází ze ZArchitecture 121 a zachovává opravu `WithTags`.

## Ověření

Kontrola existence extension metody a namespace a integrity ZIP. Build/testy nebyly spuštěny (není dostupné .NET SDK).

```bash
dotnet test SignFakeArchitecture.sln
```
