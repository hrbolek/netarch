# EntityUITemplate

Generuje samostatný Razor Class Library `EntityName.UI.csproj` a výchozí entity komponenty/stránky.

```bash
dotnet new entity-ui -n Document -o src/Document.UI --rootNamespace WebAppArchitecture.Dokument --title "Dokument"
```

Typicky se dolaďují `MediumCard.razor`, `LargeCard.razor` a `EditableCard.razor`.
