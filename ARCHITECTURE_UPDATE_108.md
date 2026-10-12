# ZArchitecture 108 — Oprava CS0246 v Core.SecurityLayer

## Příčina
Po přesunu `SubmissionNodeModel` do `DigitalProcesses.Models` zůstal `ITrustedSubmissionSnapshotReader` s implementací `TrustedSubmissionSnapshotReader` v `Core/SecurityLayer`, čímž Core nepřípustně odkazovalo na doménový typ DigitalProcesses.

## Změny
- Přesun `ITrustedSubmissionSnapshotReader` a `TrustedSubmissionSnapshotReader` do `src/Apps/DigitalProcesses/DigitalProcesses.Services/Security/` a namespace `WebAppArchitecture.DigitalProcesses.Security`.
- Doplněn import `WebAppArchitecture.DigitalProcesses.DBLayer` pro `SubmissionNodeModel`.
- Odstraněna registrace z `CoreModule` a přesunuta do `DigitalProcessesModule`.
- `RequestService` již má příslušný namespace v `using`, další úprava nebyla nutná.
- Nezavádí se reference Core → DigitalProcesses. Nezměněna sémantika privilegovaného čtení (pouze interní systémová operace).

## Ověření
Staticky kontrolováno umístění typu, používání importů, DI a absence původního odkazu v Core. Kontrola integrity ZIP. Build/test vyžadují dostupné .NET SDK (v tomto prostředí chybí).
