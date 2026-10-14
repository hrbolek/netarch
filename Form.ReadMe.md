# Formuláře, workflow a napojení na DigitalProcess

## Co obsahují soubory

| Soubor | Zdroj | Použití |
| --- | --- | --- |
| [Form.Material.json](./Form.Material.json) | [vzor.1.html](./vzor.1.html) | Vstupní struktura formuláře materiálu a služeb |
| [Form.ZSC.json](./Form.ZSC.json) | [vzor.ZSC.html](./vzor.ZSC.html) | Vstupní struktura zahraniční služební cesty |
| [Workflow.Material.json](./Workflow.Material.json) | Procesní mapa a historie v `vzor.1.html` | 18 stavů a 17 přechodů |
| [Workflow.ZSC.json](./Workflow.ZSC.json) | Procesní mapa a historie v `vzor.ZSC.html` | 9 stavů a 7 přechodů |

`Form.*.json` jsou přímo těla **vytvoření FormDefinition** i **aktualizace
FormVersion** přes `PUT /digital-processes/FormVersionResponse/tree/{id}`.
Endpoint přijímá buď `children` s `renderer_key`, nebo původní bulk pole `nodes`
s GUID typů a uzlů. Oba formáty nelze kombinovat v jednom těle.

`Workflow.*.json` jsou přenositelné importní specifikace s logickými klíči stavů.
Lze je poslat přímo na **PUT `/digital-processes/WorkflowVersionResponse/definition/{id}`**.
Pro vytvoření verze je přijímá také **POST
`/digital-processes/WorkflowVersionResponse/create-contained`**, doplněné o
`workflow_definition_id`, `version` a volitelné `publish`.
Dokumentační metadata server ignoruje.

## Důležitá omezení před importem

- Přímo pod kořenem formuláře smějí být pouze uzly `renderer_key: "section"`.
  Tyto horní sekce se zobrazují jako záložky; viditelná je pouze aktivní záložka.
  Vnořená opakovaná sekce bez podřízených sekcí může mít
  `configuration: { "visualization": "table" }` a `max_occurs > 1`.
  Jeden řádek pak odpovídá jednomu opakování, buňka se edituje po kliknutí.
  U vyhledaných hodnot se zobrazuje `label` uloženého JSON objektu.
  Výchozí vnořené zobrazení je `"stacked"`. Podrobnosti jsou v
  [dokumentaci rendererů](./src/Apps/DigitalProcesses/DigitalProcesses.UI/Renderers/README.md).
- Upravovat lze pouze **nepublikované** verze. Publikovanou verzi neměňte;
  vytvořte další draft v odpovídající definici.
- Nový DigitalProcess již vlastní FormDefinition, WorkflowDefinition i jejich
  první draftové verze. **Importujte do těchto verzí.** Samostatně vytvořenou cizí
  FormDefinition nelze libovolně přiřadit: služba kontroluje vlastnictví definic.
- Importujte před vytvářením skutečných Requestů. Již existující testovací Request
  se automaticky nerozšíří o nová pole. Po importu vytvořte nový test procesu.
  Automatický test vytvořený spolu s procesem stále odkazuje na původní obsah;
  přejmenování výchozích stavů a přechodů může změnit i popisky jeho historie.
- Hromadný update formuláře je atomický, ale **vynechané uzly nemaže**.
  Hromadný update workflow je naopak **přesná atomická náhrada**: vynechané
  přechody odstraní před vynechanými stavy. ID existujících položek zachová podle
  `key`. Změna klíče znamená odstranění staré a vytvoření nové položky.
  Mazání položek používaných žádostmi, historií nebo pravidly odmítne bez
  částečných změn. Při chybě zkontrolujte odpověď serveru.
- `id` v těle aktualizace formuláře musí být ID existujícího FormNode,
  **nikoli** jeho `key`, staré číselné ID z HTML ani ID SubmissionNode.
- Workflow soubory neobsahují původní osoby, jejich ID ani data žádostí.
  `legacy_state_id` je jen dokumentace původního `data-state`, ne databázové GUID.
- Nastavení rolí, WorkflowStateAccessRule a přístupu k polím není součástí importu.
  Podmínky a externí akce nejsou z HTML známy; `condition_query` a `action_query`
  jsou `null`. Názvy „Schváleno“ či „Vytisknuto“ samy nic neautorizují ani netisknou.

## 1. Připojení a autentizace

Příklady jsou pro Bash, `curl` a `jq` 1.6+. Spouštějte je z kořene repozitáře.
`BASE` je URL aplikace **bez** `/digital-processes`.

```bash
set -euo pipefail
BASE='https://adresa-vasi-aplikace'
BASE="${BASE%/}"
# Cookie jar z legitimně přihlášené relace; případně nahraďte CURL_AUTH podle
# autentizačního mechanismu svého nasazení.
COOKIE_JAR='/bezpecna/cesta/session.cookies'
CURL_AUTH=(--cookie "$COOKIE_JAR")

api() {
  curl --silent --show-error --fail-with-body \
    "${CURL_AUTH[@]}" -H 'Accept: application/json' "$@"
}
```

Port 8080 ani přímé volání backendu nejsou anonymní API. Použijte autentizovanou
cestu přes příslušný proxy server. Nepřidávejte do produkčních požadavků
podvrženou identitu přes `x-ms-client-principal-id`.
Požadující uživatel musí mít odpovídající Read/Create/Update oprávnění.
Nepoužívejte `curl -L`: přesměrování na přihlášení nesmí vypadat jako úspěšný JSON.
Soubory s cookies, zálohami a odpověďmi serveru necommitujte.

## 2. Vytvoření samostatného formuláře (volitelné)

Pokud chcete jen samostatný formulář, lze soubor poslat beze změny:

```bash
FORM_FILE='Form.Material.json' # nebo Form.ZSC.json
api -X POST "$BASE/digital-processes/FormDefinitionResponse/create-typed" \
  -H 'Content-Type: application/json' --data-binary "@$FORM_FILE" \
  > /tmp/form-created.json
jq -e '.id' /tmp/form-created.json
```

Vznikne nová definice i první nepublikovaná verze se zadaným stromem.
Tuto variantu **nepoužívejte pro naplnění formuláře vlastněného novým procesem**:
vytvořila by další, nezávislou definici.

## 3. Nový proces a identifikátory jeho verzí

Proces můžete vytvořit v UI, nebo API:

```bash
api -X POST "$BASE/digital-processes/ProcessDefinitionResponse/create-typed" \
  -H 'Content-Type: application/json' \
  --data '{"name":"Pořízení materiálu","description":"Import formuláře a workflow"}' \
  > /tmp/process-created.json
PROCESS_ID=$(jq -er '.id' /tmp/process-created.json)
FORM_DEFINITION_ID=$(jq -er '.form_definition_id' /tmp/process-created.json)
WORKFLOW_DEFINITION_ID=$(jq -er '.workflow_definition_id' /tmp/process-created.json)

WHERE=$(jq -cn --arg id "$PROCESS_ID" '{process_definition_id:{_eq:$id}}')
api --get "$BASE/digital-processes/ProcessDefinitionVersionResponse/list" \
  --data-urlencode "where=$WHERE" --data-urlencode 'take=200' \
  > /tmp/process-versions.json
PROCESS_VERSION_ID=$(jq -er 'max_by(.version).id' /tmp/process-versions.json)
FORM_VERSION_ID=$(jq -er 'max_by(.version).form_version_id' /tmp/process-versions.json)
WORKFLOW_VERSION_ID=$(jq -er 'max_by(.version).workflow_version_id' /tmp/process-versions.json)
```

U existujícího procesu zadejte ID požadované **draftové** procesní verze a načtěte:

```bash
api "$BASE/digital-processes/ProcessDefinitionVersionResponse/view/$PROCESS_VERSION_ID" \
  > /tmp/process-version.json
jq -e '.is_published == false' /tmp/process-version.json
FORM_VERSION_ID=$(jq -er '.form_version_id' /tmp/process-version.json)
WORKFLOW_VERSION_ID=$(jq -er '.workflow_version_id' /tmp/process-version.json)
```

## 4. Aktualizace existující FormVersion z Form.*.json

### Přímé odeslání souboru (doporučeno)

```bash
FORM_FILE='Form.ZSC.json' # nebo Form.Material.json
api -X PUT "$BASE/digital-processes/FormVersionResponse/tree/$FORM_VERSION_ID" \
  -H 'Content-Type: application/json' --data-binary @"$FORM_FILE" \
  > /tmp/form-after.json
```

Backend vyhledá typy podle `renderer_key`, převede objekt `configuration` na
uložený JSON a přiřadí existující uzly podle `(parent_id, key)`. Opakované odeslání
zachovává ID. Nové uzly vytvoří; vynechané uzly **zachová**, na rozdíl od přesné
náhrady workflow. Přesun, přejmenování klíče ani změna typu nejsou součástí importu.
Kořenové `name` a `description` se při aktualizaci stromu ignorují; nemění definici.
Platí stejné validace jako při vytvoření: maximálně 400 uzlů, hloubka 20,
platné klíče, renderer, konfigurace a limity opakování.

Odpověď je 200 s `{form, nodes}`; neplatné tělo (včetně chybějících
`nodes` i `children`) vrací 400 s vysvětlením, nepřístupná verze/uzel 404,
zakázaná mutace 403 a publikovaná verze či nepovolená změna typu 409.

### Volitelný ruční převod do původního bulk formátu

Následující alternativní postup již pro přímý import není potřeba. Použijte ho,
pokud chcete před odesláním explicitně pracovat s GUID uzlů a typů.

### Načtení stromu a katalogu

```bash
FORM_FILE='Form.Material.json' # pro cestu Form.ZSC.json
api "$BASE/digital-processes/FormVersionResponse/tree/$FORM_VERSION_ID" \
  > /tmp/form-before.json
api "$BASE/digital-processes/form-component-catalog" > /tmp/form-catalog.json
jq -e '.form.is_published == false' /tmp/form-before.json
```

### Převod do bulk requestu

Vstupní `children` je objekt s `renderer_key`. Bulk API používá pole `nodes`,
`node_type_id` a JSON konfiguraci jako **řetězec** `configuration_json`.
Převod níže hledá existující uzly podle dvojice `(parent_id, key)`, zachová jejich
GUID a nové uzly nechá vytvořit backendem. Proto je použitelný i při opakovaném
importu; po každém importu však znovu načtěte `/tree`.
Není to nástroj pro přejmenování, přesun ani změnu typu: to se provádí v designeru.

```bash
jq --slurpfile current /tmp/form-before.json \
   --slurpfile catalog /tmp/form-catalog.json '
  def convert($items; $parent):
    [$items | to_entries | to_entries[] |
      .key as $position | .value.key as $key | .value.value as $input |
      [$current[0].nodes[] | select(.parent_id == $parent and .key == $key)] as $matches |
      (if ($matches|length) > 1 then error("Duplicate existing key: " + $key)
       else ($matches[0] // null) end) as $old |
      [$catalog[0].components[] | select(.renderer_key == $input.renderer_key)] as $types |
      (if ($types|length) != 1 then error("Unavailable/ambiguous renderer: " + $input.renderer_key)
       else $types[0].id end) as $type |
      if $old != null and $old.node_type_id != $type then
        error("Cannot change node type for " + $key)
      else {
        id: ($old.id // null), key: $key, node_type_id: $type,
        label: ($input.label // $key), description: ($input.description // null),
        position: ($input.position // $position),
        min_occurs: ($input.min_occurs // 1), max_occurs: ($input.max_occurs // 1),
        configuration_json: (($input.configuration // {}) | tojson),
        children: convert(($input.children // {}); ($old.id // "__new_parent__"))
      } end];
  {nodes: convert(.children; null)}
' "$FORM_FILE" > /tmp/form-update.json

jq -e '.nodes | type == "array"' /tmp/form-update.json
api -X PUT "$BASE/digital-processes/FormVersionResponse/tree/$FORM_VERSION_ID" \
  -H 'Content-Type: application/json' --data-binary @/tmp/form-update.json \
  > /tmp/form-after.json
jq '.nodes | length' /tmp/form-after.json
```

Po vytvoření procesu zůstane výchozí sekce `section` s polem `text`: není obsažena
v importu, proto ji bulk API zachová. Pokud ji nechcete, odstraňte **pouze tento
ověřený výchozí podstrom** v designeru. Neprovádějte plošné mazání podle rozdílu
klíčů; další pole mohou být záměrná a mít vazby na Requesty či pravidla workflow.

Test formuláře: `/digital-processes/FormVersionResponse/test/{FORM_VERSION_ID}`.
Vyhledávání potřebuje dotaz podporovaný vaším GraphQL schématem. V ukázkách
formulářů je původní `userByPattern`; podle nasazení ho nahraďte ověřeným `userList`.
Musí vracet `data.choices` s `id`, `label` a přijímat proměnnou `fragment`.
Řetězec dotazu v JSON musí mít escapované konce řádků (`\n`).

## 5. Co lze z HTML říci o workflow

### Materiál

Mapa ukazuje schvalování od žadatele přes odpovědného řešitele, pověřenou osobu,
proděkana/ředitele, majetkové oddělení, rozpočtáře a zakázkové oddělení až k
vyhlášení zakázky, smlouvě, doručení, vyzvednutí a ukončení. Historie obsahuje
také „Vráceno“ u Zakázkáře a následné opakování schvalování.

`Workflow.Material.json` zachovává všechny viditelné stavy včetně Stornováno.
Cíl vrácení k žadateli a povinná poznámka jsou **návrh**, ne prokázaná konfigurace.
Názvy posledních dvou akcí jsou také návrh, protože historie je ještě neobsahuje.

### Zahraniční služební cesta

Historická cesta je žadatel → odpovědný řešitel → pověřená osoba →
proděkan/ředitel → PVEČ → Ř OE → Pracoviště OE (cesty) → ukončeno.
V historii je akce „Vytisknuto“, ale mapa zároveň označuje PVEČ jako
**„dočasně zrušeno“**. Soubor proto zachovává historický průchod a výslovně
upozorňuje na rozpor. Před nasazením potvrďte, zda se má PVEČ přeskakovat.

V obou souborech je `cancelled` zatím bez vstupních přechodů: HTML dokládá stav
a dialog, nikoli to, ze kterých stavů kdo smí stornovat. Ani návratové přechody
do všech předchozích kroků, ani zamítání, větvení či paralelismus nejsou domýšleny.
Před produkčním použitím ověřte přechody, doplňte pravidla přístupu a externí akce.

## 6. Použití Workflow.*.json pro existující WorkflowVersion

Klíče `draft`, `submitted`, `submit` navazují na výchozí obsah nového procesu.
Materiál používá také `return`; ten změní z výchozího „submitted → draft“ na
vrácení od zakázkáře. To je vhodné **před** reálným používáním, ne pro přepsání
workflow, podle kterého již proběhly žádosti.

JSON obsahuje `states` a `transitions`; `source_key`/`target_key` se překládají na
GUID z cílové verze. Metadata `source`, `limitations`, `evidence`, `legacy_state_id`
server ignoruje. Nejde o GUID přenos z jiného prostředí.

### Načtení a bezpečnostní kontroly

```bash
WORKFLOW_FILE='Workflow.Material.json' # pro cestu Workflow.ZSC.json
api "$BASE/digital-processes/WorkflowVersionResponse/designer/$WORKFLOW_VERSION_ID" \
  > /tmp/workflow-before.json
jq -e '.version.is_published == false' /tmp/workflow-before.json
# Klíče musí být jednoznačné a přechody musí odkazovat na definované stavy.
jq -e '
  ([.states[].key] | length == (unique|length)) and
  ([.transitions[].key] | length == (unique|length)) and
  ([.states[] | select(.is_initial)] | length == 1) and
  (. as $w | all(.transitions[];
    . as $t | any($w.states[]; .key == $t.source_key) and
              any($w.states[]; .key == $t.target_key)))
' "$WORKFLOW_FILE"
# Zobrazit existující prvky, které import ODSTRANÍ (pokud nemají závislosti).
jq --slurpfile desired "$WORKFLOW_FILE" '{
  extra_states: [.states[] | select(.key as $k | $desired[0].states | all(.[]; .key != $k)) | {id,key,is_initial}],
  extra_transitions: [.transitions[] | select(.key as $k | $desired[0].transitions | all(.[]; .key != $k)) | {id,key}]
}' /tmp/workflow-before.json
```

Backend ověřuje unikátní neprázdné klíče, názvy a odkazy přechodů.
Draft smí mít nejvýše jeden počáteční stav; publikování vyžaduje právě jeden.
U ZSC se výchozí přechod `return` odstraní, protože jej soubor neobsahuje.
Pokud je použitý v historii automatického testu, server import odmítne (409).
V takovém případě vytvořte další draft a napojte ho na novou procesní verzi.
Pokud nelze starý obsah bezpečně změnit kvůli historii, pracujte v nové draftové
WorkflowVersion stejné WorkflowDefinition.

Novou verzi formuláře nebo workflow lze založit také API. Předem zjistěte
nepoužitá pořadová čísla v jejich definicích:

```bash
NEXT_FORM_VERSION=2
NEXT_WORKFLOW_VERSION=2
jq -n --arg definition "$FORM_DEFINITION_ID" --argjson version "$NEXT_FORM_VERSION" \
  '{form_definition_id:$definition,version:$version,publish:false}' \
  > /tmp/form-version-create.json
api -X POST "$BASE/digital-processes/FormVersionResponse/create-contained" \
  -H 'Content-Type: application/json' --data-binary @/tmp/form-version-create.json \
  > /tmp/form-version-created.json
FORM_VERSION_ID=$(jq -er '.id' /tmp/form-version-created.json)

# Na rozdíl od vytvoření bez states/transitions se zde starší obsah NEKOPÍRUJE.
jq --arg definition "$WORKFLOW_DEFINITION_ID" --argjson version "$NEXT_WORKFLOW_VERSION" \
  '. + {workflow_definition_id:$definition,version:$version,publish:false}' "$WORKFLOW_FILE" \
  > /tmp/workflow-version-create.json
api -X POST "$BASE/digital-processes/WorkflowVersionResponse/create-contained" \
  -H 'Content-Type: application/json' --data-binary @/tmp/workflow-version-create.json \
  > /tmp/workflow-version-created.json
WORKFLOW_VERSION_ID=$(jq -er '.id' /tmp/workflow-version-created.json)
```

Formulářová verze kopíruje starší obsah včetně nových GUID uzlů. Pro formulář
znovu proveďte načtení a import v části 4. Workflow je výše vytvořeno rovnou
s kompletní definicí; další import již nepotřebuje. Pokud při vytvoření workflow
vynecháte oba atributy `states` a `transitions`, zachová se původní chování:
zkopíruje se předchozí verze včetně pravidel přístupu. Při posílání grafu musí být
přítomna obě pole; prázdná pole znamenají prázdný graf. Limit je 400 stavů
a 1600 přechodů. Vytvoření grafu i případné publikování jsou v jedné transakci.
Pro napojení nových verzí vytvořte další procesní verzi podle části 7.
Při samotné úpravě již existujícího draftu tento volitelný krok vynechte.

### Aktualizace celé existující verze

Jedním požadavkem odešlete celý soubor. Backend převádí `source_key`/`target_key`
na GUID sám a odpovídá **200** s `{version, states, transitions}`.
Publikovanou verzi nebo zakázané odstranění odmítne jako **409**, neplatnou
definici jako **400**, neexistující či nepřístupnou verzi jako **404** a
odepřenou mutaci položky jako **403**. Pravidla přístupu existujících zachovaných
stavů se nemění; nejsou součástí tohoto kontraktu.
Nepouštějte souběžně s úpravami v designeru.

```bash
api -X PUT "$BASE/digital-processes/WorkflowVersionResponse/definition/$WORKFLOW_VERSION_ID" \
  -H 'Content-Type: application/json' --data-binary @"$WORKFLOW_FILE" \
  > /tmp/workflow-after.json
jq '{version, state_count:(.states|length), transition_count:(.transitions|length)}' \
  /tmp/workflow-after.json
```

Designer výsledku: `/digital-processes/WorkflowVersionResponse/designer/{WORKFLOW_VERSION_ID}`.

## 7. Napojení na proces, test a publikování

### Pokud jste naplnili první verze vlastněné novým procesem

Odkazy již existují: první ProcessDefinitionVersion odkazuje na stejné
`FORM_VERSION_ID` a `WORKFLOW_VERSION_ID`. Nic dalšího nepřiřazujte.
Vytvořte **nový** testovací Request pomocí:

```bash
api -X POST "$BASE/digital-processes/ProcessDefinitionVersionResponse/test/$PROCESS_VERSION_ID" \
  -o /tmp/process-test-response.txt -w 'HTTP %{http_code}, navigace: %{redirect_url}\n'
```

Očekává se 302 na test této procesní verze s `requestId`. Otevřete tuto adresu
v přihlášeném prohlížeči. Test dovoluje i draftové verze; běžný Request nikoli.

### Pokud potřebujete jiné verze než ty, na které již procesní verze odkazuje

API aktuálně nemá samostatný update odkazů existující ProcessDefinitionVersion.
Vytvořte novou draftovou procesní verzi přes následující endpoint; obě zvolené verze
musí stále patřit do definic vlastněných týmž procesem:

```bash
NEXT_PROCESS_VERSION=2 # zvolte nepoužité pořadové číslo
TARGET_GROUP_TYPE_ID='GUID-povoleneho-typu-cilove-skupiny'
jq -n --arg process "$PROCESS_ID" --arg form "$FORM_VERSION_ID" \
  --arg workflow "$WORKFLOW_VERSION_ID" --argjson version "$NEXT_PROCESS_VERSION" \
  --arg target "$TARGET_GROUP_TYPE_ID" '{
    process_definition_id:$process, form_version_id:$form,
    workflow_version_id:$workflow, version:$version, publish:false,
    target_group_type_id:$target
  }' > /tmp/process-version-create.json
api -X POST "$BASE/digital-processes/ProcessDefinitionVersionResponse/create-contained" \
  -H 'Content-Type: application/json' --data-binary @/tmp/process-version-create.json \
  > /tmp/process-version-created.json
PROCESS_VERSION_ID=$(jq -er '.id' /tmp/process-version-created.json)
```

Pro produkční spuštění nejprve potvrďte rekonstruované workflow, nastavte role,
přístup k polím, podmínky a cílový kontext. Publikujte v pořadí:

1. `POST /digital-processes/FormVersionResponse/publish/{FORM_VERSION_ID}`
2. `POST /digital-processes/WorkflowVersionResponse/publish/{WORKFLOW_VERSION_ID}`
3. `POST /digital-processes/ProcessDefinitionVersionResponse/publish/{PROCESS_VERSION_ID}`

Publikování procesní verze vyžaduje publikovaný formulář, workflow a platný
`target_group_type_id`. První automaticky založená procesní verze jej nemusí mít;
v takovém případě použijte výše uvedené vytvoření další procesní verze se správným
typem cílové skupiny. Neupravujte publikovanou verzi ani databázi napřímo.
Samotný import žádnou verzi automaticky nepublikuje.
