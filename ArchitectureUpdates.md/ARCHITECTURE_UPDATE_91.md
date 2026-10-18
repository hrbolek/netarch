# ZArchitecture 91 – jednotné RBAC zobrazení a správa Root

- Návrhář formulářů i workflow nově zobrazují sdílenou `EntityPermissions` komponentu (`rbacobject_id` příslušné verze).
- Nová stránka `GET /architecture/root-permissions` zobrazuje kořenovou skupinu a čtyři speciální skupiny pro vytváření entit.
- Přidávání rolí využívá stávající zabezpečené `create-role-action` / `create-role-form` endpointy, jejich autorizace se nemění.
- Vstup na administrační stránku vyžaduje `Read` nad kořenovou skupinou. `Create` je požadováno při přiřazování rolí.
- Odkazy na `RoleTypeResponse/list` slouží k úpravě typů rolí a operací existujícími nástroji.
- Nejsou přidány žádné výjimky pro bootstrap administrátora, žádné nové migrace databáze.
- Pozor: stránka podporuje prohlížení oprávnění a přidávání rolí, **nikoli přímou změnu již existujících role assignments v místě**. K tomu je potřeba rozšířit stávající Role edit endpointy.

## Ověření

Není dostupný .NET SDK, proto nebylo možné sestavit Razor projekt a spustit testy. Po nasazení spustit `dotnet test SignFakeArchitecture.sln -c Debug` a zkontrolovat `GET /architecture/root-permissions`.
