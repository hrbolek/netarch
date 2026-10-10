# TargetGroupType – stav implementace a akceptační kritéria

## Již připravené (DP19–DP23)

Čistý policy kontrakt `ProcessTargetGroupPolicy` a pět testů. Politika umožňuje explicitní cílové RBAC skupiny nebo pravidlo podle `GroupType`, ale **nepřiznává** uživateli žádné oprávnění. Bez pravidel odmítá všechny cíle. Neprohledává potomky ani hierarchii typů, protože jejich sémantika ještě nebyla dohodnuta.

## Zbývající integrační práce – podmínky pro uzavření DigitalProcesses

1. Perzistentní `ProcessTargetGroupRule` navázat na neměnnou `ProcessDefinitionVersion` s přesně jedním z `TargetGroupId` / `TargetGroupTypeId`, nastavit databázový invariant a publikační validaci.
2. Rozšířit `StartRequestRequest` o požadovaný `TargetRbacObjectId` a volitelnou cestu pro jednoznačný cílový kontext uživatele.
3. Při `StartAsync`: načíst cílovou skupinu a publikovaná pravidla **na serveru**, ověřit `IsAllowed`, potom nezávisle `Create` právo skutečného volajícího; neautorizované cíle vracet bez vedlejších efektů.
4. Request RBAC hranici vytvořit pod vybranou cílovou skupinou a uchovat stabilní `TargetRbacObjectId` v Requestu. Samostatně aplikovat state-scoped RoleType Read/Write/Update a Deny pro FormNodes; žádná Membership->Role vazba.
5. Pozitivní E2E fixture: dvě fakulty stejného typu, třetí nepovolená cílová skupina; žadatel s Create, referent s Read/Update, jiný uživatel bez práv; workflow transition archivuje první Submission a vytváří druhý, tajné hodnoty přežijí snapshot, ale Deny je skryje z API.
6. Verzování cílových pravidel: nová ProcessVersion nesmí měnit cílový kontext již běžícího Request.
7. Negativní HTTP a DB testy: nepovolený typ, povolený typ bez Create, žádné cíle, neoprávněný Read/Write/Deny, souběžné přechody, žádný částečný zápis.

**Nesmí se zaměnit**: úspěšné DP19–DP23 dokazují pouze algoritmus výběru cíle, nikoli celé propojení s `StartAsync` nebo autorizaci dat.
