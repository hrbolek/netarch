# ARCHITECTURE_UPDATE_81 — DigitalProcesses: správa šablon a Applicant Deny matice

## Zavedené funkce

- Nová stránka `/digital-processes/process-versions/{id}/permissions`: přehledná matice **WorkflowState × FormNode**, s volbou `Read` / `Deny` pro roli Applicant. Zobrazuje hierarchii formuláře, stav publikace a používá existující autorizovaný API endpoint `ProcessDefinitionVersionResponse/applicant-access/{id}`. Publikované verze se editovat nedají.
- Runtime v `RequestService` a `SubmissionNodeService` již vynucuje Applicant Deny pomocí hierarchie oprávnění a `RbacDenyRule`; pravidla se tímto UI pouze spravují. Žádné přímé zápisy do DB z browseru.
- Nová stránka `/digital-processes/form-versions/{id}/templates` umožňuje vidět, stáhnout, nahrát a vygenerovat RTF šablonu na úrovni FormVersion. Odkazy jsou v přehledu verzí a kartě FormVersion.
- AI pro zpracování vzorového dokumentu zatím zůstává viditelným, neaktivním placeholderem. Schéma formuláře a API pro revize vznikly v předchozí verzi.
- Odkazy na matice oprávnění jsou v přehledu ProcessVersion a detailní kartě.

## Důležitý rozsah a omezení

- **Matice v 81 konfiguruje pouze Applicant**. WorkflowStateAccessRule a RBAC vazby pro ostatní role nadále existují, ale UI ještě nenabízí obecný explicitní Deny pro libovolnou roli. Před rozšířením je potřeba doplnit odpovídající runtime politiku a její testy.
- Stav `Dědit / neurčeno` v matici pouze ukazuje, že explicitní pravidlo neexistuje; zatím neobsahuje DELETE akci pro zrušení již uloženého pravidla.
- Stránka šablon využívá existující RTF API. Má historii revizí dostupnou jako JSON, nikoliv ještě vizuální revizní editor a mapování placeholderů.
- Formulářová UI jsou závislá na autentizaci a existujícím `ISecurityLayer`; neobcházejí pravidla oprávnění.
- Nevznikají nové databázové tabulky, tedy není potřeba spouštět SQL migraci.

## Ověření

Provedena kontrola struktury zdrojů a návaznosti na existující endpointy. V tomto prostředí není dostupný `dotnet`; kompilaci a integrační testy 81 je třeba ověřit v Dev Containeru:

```bash
dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug
```

## Další verze

1. Generalizovat matici na libovolný `RoleType` a explicitní Deny pro `Read`/`Write`, s odpovídajícím runtime vynucováním a testy.
2. Přidat DELETE explicitního pravidla a hromadnou editaci sloupce/řádku.
3. Vizuální editor revizí RTF a vazeb placeholderů.
4. Produkční dokumentovou akci na Submission a integrační test multipart JSONata importu.
