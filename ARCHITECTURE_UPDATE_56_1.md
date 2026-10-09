# ZArchitecture56.1 — oprava kompilace kontraktů DigitalProcesses

Chyby CS0246 ukazovaly na tři modelové typy ve dvou souborech DigitalProcesses.Models.
Typy se nemění; byly přesunuty do samostatných modelových zdrojových souborů:

- `Models/FormBackendContracts.cs`: `FormBackendParameterBinding`, `FormBackendOperationRequest`, `FormBackendParameterSource`.
- `Models/FormDefinitionDetailResponse.cs`: `FormDefinitionDetailResponse`.

Původní definice byly odstraněny, aby nedocházelo ke konfliktům a aby se projevil nový build vstup modelové knihovny. Verze Models projektu je zvýšena pro obnovení inkrementálních výstupů.

Ve vývojovém kontejneru spusťte `bash tools/rebuild-digital-processes.sh`, případně `dotnet clean` a `dotnet build`.
Bez lokálního .NET SDK nebylo možné sestavení zde ověřit. Jde o opravu distribučních/kompilačních vstupů, nikoli změnu chování DummyJsonataCaller.
