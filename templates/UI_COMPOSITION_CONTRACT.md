# Contract: Core Razor composition

The required self-contained examples are in `EntityUITemplate/Pages/*.razor` and `EntityUITemplate/Components/*.razor`; use them with this contract, independent of external Core source. Verify public generic component parameters from the installed binaries.

Composition map: **list** = `PageViewList` + domain `MediumCard`; **detail** = `PageView` + domain `LargeCard`; **edit** = `PageEdit` + domain `EditableCard` in large-card context; **create** = `Page` + context-validated form and field controls; **relations** = `EntityLink` / `LazyEntityLink` and vector components; **RBAC actions** = `EntityPermissions` / `EntityLinks` and secured `ActionsUrl`.

## Rules

1. Every domain entity has a domain `MediumCard` and `LargeCard` wrapper over generic Core components when a detail/list UI is required. Do not create replacements for Core `MediumCard`/`LargeCard`.
2. Standard list/detail/edit pages are **thin adapters** with typed `Model` and, where required, `IsFragment`; only special content belongs inside cards.
3. `LargeCard.SideContent` composes `MediumCard`, related navigation (`EntityLinks`), and `EntityPermissions` where applicable. Relations are rendered using `EntityLink`, `LazyEntityLink` and `ViewVector`/domain vector content as supported.
4. `MediumCard.ActionsUrl` must point to the registered, RBAC-aware `.../actions/{id}` endpoint. No unconditional Edit/Delete links.
5. Create is distinct from edit and may require a context group; `Page` + `EntityLookupSelect` is valid where the Create security contract requires it. Never assume a universal generic Create page.
6. Honor the current `ApplyViewModel`/`ApplyJsonModel` integration and default `IsFragment` semantics. No independent JavaScript app or HTML string concatenation.
7. Cross-domain links use existing entity link infrastructure. Simple static anchors may occur in references; prefer typed links when possible, without claiming every existing reference is perfectly uniform.

**Important:** `COREUI_COMPOSITION.md` in the old `EntityUITemplate` previously contradicted the actual Core patterns; this document supersedes it. Whether a new specialized `LargeCard` is necessary depends on required domain content, but a domain-specific card is shown by the included UI template.

## Editable field composition

`Create` and `Update` UI also follow [`INPUT_COMPONENTS_CONTRACT.md`](INPUT_COMPONENTS_CONTRACT.md). Use the existing `EntityLookupSelect` for editable FK relationships and `EntityLink` for readonly navigation. No new ad hoc FK widgets; do not imply missing Core scalar Input controls already exist.

Razor pages and entity-specific UI components no longer use architecture
annotations, including generated page and card templates. Preserve their typed
component parameters; service models and transport contracts do not become UI
types merely because a page consumes them.
