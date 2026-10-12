# ARCHITECTURE UPDATE 113

## Oprava CS0246 v User.Resolvers

- `UserResolvers.cs`: doplněn `using WebAppArchitecture.Group.Models;`, protože `Actions` používá `VectorAsync<GroupResponse>`.
- `User.Resolvers.csproj`: doplněna explicitní reference na `Group.Models.csproj`.
- Nedošlo ke změně RBAC, transakcí ani HTTP kontraktů.

## Ověření

- Kontrola existence `GroupResponse` a namespace.
- Kontrola syntaktické správnosti XML projektu a integrity ZIP.
- `dotnet build` nebyl spuštěn (SDK není v tomto prostředí k dispozici).

## Instalace

Archiv doporučujeme rozbalit do čistého adresáře, protože extrakce přes starší verze nemaže odstraněné soubory.
