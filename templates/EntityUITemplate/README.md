# EntityUITemplate

Generuje samostatný Razor Class Library `EntityName.UI.csproj` a výchozí entity komponenty/stránky.

```bash
dotnet new entity-ui -n Document -o src/Document.UI --rootNamespace MyApp --title "Dokument"
```

Typicky se dolaďují `MediumCard.razor`, `LargeCard.razor` a `EditableCard.razor`.
