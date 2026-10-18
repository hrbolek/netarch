---
name: razor-component-reuse
description: Build ZArchitecture pages from typed Razor models and existing shared UI components without moving presentation into services or resolvers.
---

## When to use

Use for new or changed Razor pages, cards, entity links, lookups, permissions,
relation vectors, or visual renderers.

## Prerequisites

Inspect `src/Core/Base/UI/Components/`, `Pages/`, the owning domain's `*.UI`
project, and `ZA-UI-001` in `docs/architecture/rules.yml`. For domain-level UI,
read `src/Apps/Events/README.md` and relevant UI composition contracts.

## Procedure

1. Identify page state, typed response DTO, route, permission behavior, and
   closest existing page/component.
2. Prefer `PageView`, `PageViewList`, `PageEdit`, `MediumCard`, `LargeCard`,
   `EntityLookupSelect`, `EntityLink`/`LazyEntityLink`, `EntityPermissions`,
   `EntityLinks`, `ViewVector`, and existing vector content components when
   their contracts fit.
3. Compose visual output in Razor/UI. Keep assets and substantial renderers in
   the owning UI project; preserve public asset URLs when moving resources.
4. Keep components typed and accessible; preserve standard Bootstrap and
   read/edit permission behavior. Avoid raw HTML strings in resolvers/services.
5. Add or update behavior/component tests for navigation, empty state,
   permission-sensitive actions, rendering, and representation routing.

## Architectural constraints

- UI consumes response/view models and appropriate contracts; it does not own
  database access or become a Services dependency.
- Add a component only for a distinct reusable interaction. Do not create a
  parallel page/card system to match naming alone.

## Validation

- Build the UI and the resolver/host that serves it.
- Run relevant rendering/integration tests and check routes/assets when changed.
- For layer moves, verify compile-time project references and boundary tests.

## Expected outputs

Describe reused components, view-model/route/permission behavior, any new
reusable UI interaction, project-boundary effects, and exact validation results.
