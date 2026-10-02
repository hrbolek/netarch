# EntityModelTemplate

Generuje samostatný `EntityName.Models.csproj` s persistence modelem, response DTO a store kontraktem.

Z rootu repozitáře:

```bash
dotnet new entity-model -n Document -o src/Document.Models --rootNamespace WebAppArchitecture.Dokument
```

Projekt očekává `src/Base/Base.csproj` o jednu úroveň vedle.
