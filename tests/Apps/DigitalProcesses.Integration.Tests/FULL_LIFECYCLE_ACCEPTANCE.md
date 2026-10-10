# DP26 – skutečný životní cyklus runtime (nový test)

Nový `DigitalProcessesFullLifecycleTests.cs` založí do izolované SQLite databáze již publikovanou definici formuláře, FormNode, verzi workflow se dvěma stavy a přechodem, verzi procesu a cílovou organizační skupinu. Následně provede skutečné HTTP příkazy `POST /RequestResponse/start`, `PUT /SubmissionNodeResponse/value/{id}`, `POST /RequestResponse/transition/{id}` a pokus o úpravu archivovaného SubmissionNode. Po operacích kontroluje databázové invarianty, úplnost přenosu hodnot, novou aktivní verzi a auditní historii.

## Hranice pokrytí

- **Testuje:** runtime Start, cílový GroupType, aktivní Submission, editaci, Transition, archivaci, zachování hodnot, historii, zákaz úpravy archivovaného formuláře.
- **Dosud netestuje:** HTTP vytváření/publikování definic (fixture se ukládá přímo v DB), kompletní matici více identit a `WorkflowStateAccessRule` / `FormNodeApplicantAccess` / `Deny`, souběh dvou přechodů.
- Všechny požadavky DP26 vykonává bootstrap admin, aby se pozitivní runtime průchod neplet s testem přidělování autorizačních rolí. Tím není prokázána autorizace běžného studenta.

## Spuštění

```bash
dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug --filter "FullyQualifiedName~DP26_"
```

Po potvrzení se může test spouštět s celou aplikační sadou. Test zatím nebyl zkompilován ani spuštěn v našem prostředí (.NET SDK není instalováno).
