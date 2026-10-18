# ZArchitecture59 – kontextové editory a referenční výběry

## Analýza

Hlavními agregáty jsou ProcessDefinition, FormDefinition a WorkflowDefinition. Jejich verze a interní prvky (FormNode, WorkflowState, WorkflowTransition, WorkflowStateAccessRule) jsou závislé na kontextu nadřazené definice/verze. Samostatné položky hlavní navigace pro jejich vytváření vedly ke ztrátě kontextu. API pro tyto prvky zůstává kvůli integracím dostupné; z hlavní navigace je odstraněno. Pravidla workflow by měla být v další iteraci doplněna do konkrétního workflow state editoru; v této změně se pouze skrylo jejich samostatné menu.

## EntityLookupField

Reusable Razor component UI/Components/EntityLookupField.razor (Input, Select, Endpoint URL, Required). Odesílá dotazy na RBAC-scoped lookup endpoint. První zobrazení: 25 řazených hodnot. Zadaný filtr kratší než 2 znaky: opět neomezený filtr a první 25. Dva a více znaků: serverový substring filtr, max 25. Zpoždění 180 ms, přerušení předchozího fetch. Předávání vybrané hodnoty standardním HTML formulářem pod názvem FK.

Příklad: WorkflowStateAccessRuleResponse/create používá lookup pro workflow states, role types a form nodes. Lookup endpoint `/digital-processes/WorkflowStateAccessRuleResponse/lookup/{kind}?q=…`. Dotaz používá Security.Query(...Read) a Take(25) před materializací. Pro další entity lze použít DigitalProcessCreateForm.EntityLookup a jejich vlastní serverový lookup resolver.

## Navigace

Z hlavního menu byly odstraněny verze procesů/formulářů/workflow, FormNode, WorkflowState, WorkflowTransition a access rules (včetně přímých create akcí). Hlavní definice zůstávají. Přímá HTTP API nebyla odstraněna.

## Omezení

Nebylo možné provést dotnet build (SDK není dostupné). Tato verze neimplementuje nový editor workflow access rules uvnitř state editoru; má být následujícím krokem pro důslednou kontextovou správu. Vyhledávání používá prozatím database Contains podle provideru, nikoli fulltextové či fuzzy indexy.
