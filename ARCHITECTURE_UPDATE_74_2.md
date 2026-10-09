# ZArchitecture 74.2 — oprava Razor parseru

- `ExternalDataImportSection.razor`: víceřádkový C# raw string (`"""`) v `@code` nahrazen běžným escapovaným string literalem; opravuje RZ1000 a navazující CS0116.
- Beze změny veřejného kontraktu komponenty a konfigurace JSONata.
- Ve zdejším prostředí není k dispozici .NET SDK; doporučený test: `dotnet build src/Host/Host.csproj`.
