# ARCHITECTURE UPDATE 89

## Oprava AI FormSchema serializace kořenových uzlů

Opraven `FormDesignAiEndpoints.Design`: kořenové FormNodes mají legitimní `parent_id = null`, ale `GroupBy(...).ToDictionary(...)` se pokoušel použít null jako klíč a končil `ArgumentNullException`, HTTP 500.

Nyní se kořenové uzly shromažďují v `rootNodes` a slovník `childrenByParent` indexuje pouze nenulové Guid rodičů. Metoda `BuildChildren` vybírá kořeny při `parent == null`; u dalších úrovní používá slovník. Zachovává se stabilní UUID, validace duplicit, cyklů, neúplných oprávnění a hloubky.

**Žádná SQL migrace.**

Ověření: statická kontrola změny a integrity ZIP, nikoliv kompilace .NET nebo integrační testy.
