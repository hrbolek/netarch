# EntityModelTemplate

Generuje aplikační request/response DTO pro entitu. Persistence `EntityNameModel` musí být definován v projektu `DBLayer`; doménový `*.Models` projekt DB model nevlastní.

Z rootu repozitáře:

```bash
dotnet new entity-model -n Document -o src/Document.Models --rootNamespace WebAppArchitecture.Document
```

Projekt očekává `src/Base/Base.csproj` a `src/DBLayer/DBLayer.csproj` o jednu úroveň vedle.
