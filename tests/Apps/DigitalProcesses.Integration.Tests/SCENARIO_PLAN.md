# DigitalProcesses – plán integračních scénářů

## Rozdělení

Testy aplikace zůstávají v `tests/Apps/DigitalProcesses.Integration.Tests`. Základní RBAC v Core. SQLite je jediný provider pro běžné testy; PostgreSQL a MSSQL patří do etapy 99.

**Poznámka:** Nové testy DP01–DP03 ověřují pouze selhání bez perzistentních následků v izolované databázi. Neprokazují kladný lifecycle ani rollback po pozdější fázi transakce. Také prázdný autorizovaný seznam sám o sobě nevylučuje neautorizované skryté řádky; pro silnější test je nutná přímá kontrola DB fixture.

## Scénáře a akceptační kritéria

| ID | Oblast | Předpoklad | Akceptační kritérium | Priorita | Stav |
|---|---|---|---|---|---|
| DP01 | Start: neznámá verze | Bez procesní definice | Neúspěšná odpověď, žádné runtime záznamy viditelné přes API | P0 | Implementováno |
| DP02 | Transition: neznámý Request | Prázdná runtime DB | Neúspěšná odpověď, žádná historie ani Submission | P0 | Implementováno |
| DP03 | Opakované chybné Start | Prázdná runtime DB | Opakovaná chyba nevytváří viditelné runtime záznamy | P1 | Implementováno |
| DP04 | Publikace definice | FormDefinition, FormVersion, WorkflowDefinition/Version, stavy, přechody, ProcessDefinition/Version | Publikovanou verzi lze použít pro Start, návrh ne | P0 | Čeká na datovou fixture |
| DP05 | Start: pozitivní | DP04, iniciátor s Read a Create | Request a Submission v1, správné RBAC hranice a iniciační role | P0 | Čeká na datovou fixture |
| DP06 | FormNode hodnoty | DP05, kořen a potomci | Zápis/přečtení scalar hodnot, kontrola validace | P0 | Čeká na datovou fixture |
| DP07 | Transition pozitivní | DP06, povolený transition | Nový Submission v2; v1 archivní; current_submission_id změněno; historie obsahuje přechod | P0 | Čeká na datovou fixture |
| DP08 | Neměnnost archivu | DP07 | Archivní Submission a jeho hodnoty nelze modifikovat | P0 | Čeká na datovou fixture |
| DP09 | Deny a zachování hodnot | DP07, rozdílné state policy | Skryté hodnoty přežijí snapshot, ale nejsou vráceny neautorizovanému uživateli | P0 | Čeká na datovou fixture |
| DP10 | Více uživatelů | DP05, role iniciátora a referenta | Viditelnost dle Role/Binding/Deny; Membership nemá účinek | P0 | Čeká na datovou fixture |
| DP11 | Podmíněný transition | DP07, condition_query | Zamítnutý přechod nearchivuje a netvoří nové záznamy | P1 | Čeká na datovou fixture |
| DP12 | Neplatný transition | DP05, jiný zdrojový stav | Chyba beze změny Request/Submission/historie | P0 | Čeká na datovou fixture |
| DP13 | Opakované sekce | DP06, repeatable FormNodes | Strom, pořadí a hodnoty zachovány v novém snapshotu | P1 | Čeká na datovou fixture |
| DP14 | JSON export | DP09, různá práva | Nikdy neobsahuje zakázané uzly ani jejich hodnoty | P0 | Čeká na datovou fixture |
| DP15 | Konkurence | DP07, dvojí transition stejného Request | Nevzniknou dvě aktivní Submission ani nekonzistentní historie | P1 | Čeká na datovou fixture |

## Struktura datové fixture pro DP04–DP15

1. Vytvořit základní RBAC strom a RoleType s povolenými `Read`, `Create`, `Update`.
2. Založit `FormDefinition` a publikovat `FormVersion` se dvěma větvemi (veřejná, důvěrná).
3. Založit `WorkflowDefinition/Version`, inicializační a následný stav, přechod a pravidla čtení.
4. Založit `ProcessDefinition/Version` s `initiator_role_type_id`, oběma verzemi a `is_published=true`.
5. Ověřit přípravu přes EF Core v izolované DB; operace testovat HTTP klienty s různou identitou, nikoli přímým voláním služby.
6. U pozitivních testů ověřovat také surový stav databáze, aby neviditelný řádek nebyl mylně považován za neexistující.

Samostatně testovat validaci dat, oprávnění a integritu snapshotů. Každá aplikace drží vlastní fixture a testovací projekty. Nezavádět ručně psané SQL do DBLayer/SecurityLayer.
