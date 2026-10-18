# ZArchitecture 93 — Digital Processes: contextual pages and ordered form tree

## Changes
- `FormDefinitionResponse/view` uses the entity-owned `FormDefinitionLargeCard` with the existing versions table in ChildContent.
- `WorkflowDefinitionResponse/view` uses the entity-owned `WorkflowDefinitionLargeCard`; the workflow definition DTO is carried in `DefinitionWorkflowVersionPageModel`. Existing process version rendering remains unchanged.
- Process and workflow resolvers bind directly to `DefinitionProcessVersionPage` and `DefinitionWorkflowVersionPage` with separate domain models. Both use `DefinitionVersionsTable` for ordering, status, publishing and draft deletion; creation and version-specific links remain in the respective page. No shared page dispatches by `Model.Kind`. Forms continue to use `FormDefinitionDetailPage`.
- Form and Workflow designers use `FormVersionLargeCard` and `WorkflowVersionLargeCard`, including the contextual navigation and RBAC panel.
- Form designer structure list is ordered by depth-first traversal of sibling nodes by `(position, key)`, matching the form renderer, instead of lexicographic `path` ordering. Orphan nodes remain visible.
- Standalone template and applicant-permission HTML endpoints now have contextual 3:9 two-column cards and navigation to related pages. These are HTML equivalents and **not the Razor LargeCard component**; full unification would require routing their HTML through a Razor view.
- Applicant matrix displays diagnostic messages when there are no readable workflow states, no readable form nodes, or the current user lacks Update on the process draft.

## Known limitation
- Applicant matrix intentionally only covers Applicant Read/Deny and requires Update permission on the process version; draft alone is not sufficient.
- Resetting an explicit applicant rule to inheritance is still unsupported by the underlying API. The `Dědit / neurčeno` option must not be advertised as persistable.
- `dotnet` CLI was not available in the processing environment; build and runtime integration tests were not run. Validate with `dotnet build SignFakeArchitecture.sln` and check the listed routes.
