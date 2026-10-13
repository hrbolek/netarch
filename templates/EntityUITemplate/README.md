# Entity UI template — reference-conformant composition

**Authority:** `../UI_COMPOSITION_CONTRACT.md` and this included UI scaffold. External Core domain source is not needed.

`dotnet new entity-ui` generates **only UI scaffolding**; it does not register endpoints or provide a working edit backend by itself. Before deploying, replace `APPLICATION_PREFIX` in `EntityNameMediumCard` and in forms with the domain route, wire the secured API and add navigation.

- List: `PageViewList` + domain `EntityNameMediumCard`.
- Detail: `PageView` + domain `EntityNameLargeCard`.
- Edit: `PageEdit` + `EntityNameLargeCard` + `EntityNameEditableCard`.
- `LargeCard` delegates its sidebar to `MediumCard` and displays `EntityPermissions`.
- Show related entities with `EntityLink`/`LazyEntityLink`, collections with `ViewVector` and relation-specific vector content.
- Create pages are context-dependent. Follow `../INPUT_COMPONENTS_CONTRACT.md`; **never** hard-code an unrestricted `rbacobject_id` field.
- The example editable component is a skeleton until its action URL, form binding, validation, and save response are wired.

For a complete new entity follow `../ENTITY_CONTRACT.md`; for a new domain follow `../DOMAIN_CONTRACT.md`. Generate and build the entire host before calling the result complete.

### Input fields and foreign keys

`../INPUT_COMPONENTS_CONTRACT.md` is mandatory for Create and Update. Core currently exposes `EntityLookupSelect` for searchable FK fields, but not generic scalar `TextInput` or `DateTimeInput`. The generated `EditableCard` intentionally remains a scalar form skeleton. Adapt it to the real DTO, attach the domain endpoint and use Core `EntityLookupSelect` for every searchable FK. Do not duplicate a new picker into this UI project. Use `../INPUT_COMPONENTS_CONTRACT.md` for self-contained create-context rules and lookup selector markup.
