# Input and selector contract (normative)

## Self-contained sources of truth

- This contract defines input semantics and the complete FK example below.
- `EntityUITemplate/Components/EntityNameEditableCard.razor` is the locally distributed scalar-form scaffold; it requires real DTO and endpoint wiring.
- `EntityUITemplate/Pages` defines the page container patterns.
- `EntityLookupSelect` is a **public Core UI component**; verify its actual parameter signatures via installed assembly metadata/XML docs. Core source code is optional and not required.

Shared scalar `TextInput`, `DateTimeInput`, `EnumSelect`, `NumberInput`, `BoolInput` and `EntityMultiLookupSelect` are **not guaranteed public components** in the baseline. Do not emit nonexistent tags. Use an approved shared field abstraction when present, otherwise minimal native controls while explicitly marking the reuse gap.

## Normative rules for new entity Create / Update

1. Treat field presentation as part of the entity contract. Every editable model field must have a declared input type, posted `Name` (matching request binding), label, nullable/required semantics, validation, read-only treatment, and authorization constraints.
2. **Foreign keys**: prefer Core `EntityLookupSelect` rather than manually entering GUIDs, downloading all related rows to a static `<select>`, or hardcoding an ad hoc AJAX widget. Bind `ListUrl` to the authorized list resolver for the referenced entity; `SearchFields` must name supported searchable response properties. The displayed label is separate from the posted UUID.
3. For Update with an existing FK, set **both** `InitialId` and `InitialLabel`; support `Required=false` for nullable FKs. For fixed context relationships (e.g., parent set in an action), show human-readable context and post the ID; the server MUST validate it rather than trust a hidden field.
4. Filter candidate parent/target entities by **operation** where needed, e.g. `?operation=create` on reference endpoints. Choosing from a filtered lookup **never replaces** authorization of the final mutation in the service.
5. Create and Update should use the same field semantics and presentation primitives, but may differ in surrounding page/layout and mutation behavior. Use the existing generic `Page`/`PageEdit` and domain `EditableCard` patterns specified in `UI_COMPOSITION_CONTRACT.md` and `EntityUITemplate`; do not recreate those page containers.
6. Input components must not read EF entities or write to `DbContext`; they exchange form values and/or typed parameters. Service owns validation/business rules and transaction. Render validation errors and preserve entered values on failed writes.
7. Scalars currently use native `input`/`textarea`/`select` under consistent Bootstrap conventions. Central shared scalar controls are an **explicit future Core change**, not something silently introduced while creating a domain.
8. Date/time: specify whether the field is a local wall-clock input or an absolute instant. Convert at the boundary with explicit timezone policy; PostgreSQL `timestamptz` receives UTC. Do not infer timezone from machine-local defaults.
9. Do not allow a client-editable `rbacobject_id` to determine authority. Scope of creation must be derived and verified via server-side `CreationScopeGuard`/security service.
10. Inputs respect read/write field permissions where such policy exists, but visibility or disabled fields alone are never sufficient authorization.

## Required field mapping table in entity design

| Domain field | Data/API meaning | Input choice (current baseline) | Server behavior |
|---|---|---|---|
| Required string | text, length, required | native `input type=text` in standard form; shared component when available | validate required/length |
| Optional long string | nullable text | native `textarea` | validate limits |
| Integer/decimal | bounded number | native `input type=number` | validate range/culture |
| Boolean | true/false | checkbox with explicit false semantics | validate |
| Enum/code list | finite choices | native `select`, or specialized shared control if available | validate permitted values |
| Local date/time | local calendar value | `input type=datetime-local` | interpret using explicit timezone rule |
| Foreign key `Guid` | relation to another entity | `EntityLookupSelect` | verify target exists and caller may bind/use it |
| Optional foreign key `Guid?` | nullable relation | `EntityLookupSelect Required=false` | permit clear only if domain permits |
| Readonly related entity | navigation | `EntityLink`/`LazyEntityLink` | RBAC-aware read |
| To-many relation | relation collection | `ViewVector` for display; editor requires domain-specific operation | secure batch mutation |

## Verified API shape for Core EntityLookupSelect

```razor
<EntityLookupSelect Name="group_id"
                    Label="Skupina"
                    ListUrl="/architecture/GroupResponse/list?operation=create"
                    SearchFields="@GroupSearchFields"
                    Required="true"
                    MinChars="2"
                    Take="20"
                    InitialId="@SelectedGroupId"
                    InitialLabel="@SelectedGroupName" />
```

`GroupSearchFields`, `SelectedGroupId`, `SelectedGroupName` are supplied by the hosting component. `ParentField` and `ParentProperty` optionally scope lookup searches. `Name`, `Label`, `ListUrl`, and `SearchFields` are required arguments. When modifying this component, check the JavaScript lookup implementation and form binding compatibility as well as Razor markup.

## Review/acceptance checklist

- New entity has field-level Create and Update maps, including relation selectors and nullable/required behavior.
- Every FK control posts the entity UUID but displays a readable label; Update shows a preselected label.
- Referenced entity endpoints support pagination/search and relevant RBAC-filtering.
- Server validates scope, referenced values, mutability, and timezone conversion independently of UI.
- No per-entity alternative picker or duplicated scalar component introduced without decision and Core-wide reuse.
- Complete HTML Create and Update interaction tested, not just JSON API.

## Future architecture decision (not implemented by these templates)

A truly centralized visual/input change requires reusable **Core** scalar input components and optionally a typed field-definition contract. Design and approve these as a separate Core change, then revise EntityUITemplate to consume them. Until then, `EntityLookupSelect` is the actual reusable relationship input; the other examples remain native fields.
