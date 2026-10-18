# ZArchitecture 138 — Input/Select template contract

Changed **only** `/templates/` plus this changelog. Added `templates/INPUT_COMPONENTS_CONTRACT.md`, grounded in actual `Core/Base/UI/Components/EntityLookupSelect.razor`, `Core/Group/.../GroupCreatePage.razor`, `Core/Membership/.../MembershipCreatePage.razor` and `GroupEditableCard.razor`.

- Defines mandatory field-by-field Create and Update mapping: scalar types, enum, nullable relations, date/time and readonly links.
- `EntityLookupSelect` is the current canonical searchable FK control, with `ListUrl`, `SearchFields`, `InitialId`/`InitialLabel`, `Required`, pagination bounds and contextual filtering; lookup and final mutation both respect RBAC.
- Defines context-dependent Create, server-side FK checks, validation and UTC conversion rules.
- Integrates the contract into template entrypoints (`README.md`, `AGENTS.md`, `ENTITY_CONTRACT.md`, `UI_COMPOSITION_CONTRACT.md`, `EntityUITemplate/README.md`, `TEMPLATE_STATUS.md`).
- Explicitly records that reusable scalar field components (`TextInput`, `DateTimeInput` etc.) **are not in Core today**. They are a proposed separate architectural Core change, not a fake dependency of this template update.

**Scope / known limitations:** Templates' generated scalar EditableCard is still illustrative and is not a fully functional backend-wired Create/Update flow. No .NET compilation or integration tests performed in this environment; source archive ZIP integrity checked. Other scaffold gaps remain as described in `TEMPLATE_STATUS.md`.
