# ZArchitecture 77.5 – Import action in Design vs Test

- Importní tlačítko je v Design režimu neaktivní s jasným vysvětlením; Preview neexistuje mimo Test.
- V Test režimu je aktivní pouze s neprázdným PreviewInstanceId a PreviewFormVersionId.
- Aktivní tlačítko dál používá hx-include="#dp-preview-form" a hx-vals pro explicitní identitu.
- Test: /digital-processes/FormVersionResponse/test/{versionId}.
- Po rozbalení restartujte proces dotnet a znovu načtěte stránku, aby se obnovily Razor komponenty.
- Pozor: .NET SDK není v prostředí sestavení asistenta dostupné, nelze ověřit kompilaci.
