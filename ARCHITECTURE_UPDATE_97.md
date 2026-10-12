# ZArchitecture 97 — FK navigation in MediumCard

## Objective
The LargeCard shows the current entity. Its MediumCard must allow graph navigation along actual domain foreign keys to the corresponding entity view. Navigation actions (designer, permissions, templates, versions) remain separate in EntityLinks.

## Changes
- Added `DigitalProcessForeignKeyLinks.razor`: typed FK navigation for ProcessDefinition, FormVersion, WorkflowVersion, ProcessDefinitionVersion, FormNode (including parent), WorkflowState, WorkflowTransition, Request and Submission.
- Placed the component inside `DigitalProcessMediumCard.razor`, so links appear for every LargeCard using that card.
- Moved ProcessDefinition's direct FormDefinition/WorkflowDefinition navigation from the generic LargeCard action links into its MediumCard, avoiding duplicate destinations.
- The target URLs use `/digital-processes/{ResponseType}/view/{id}` and do not expose audit or RBAC foreign keys.

## Validation
- Source inspection and archive integrity checks performed.
- `dotnet` SDK was not available in the build environment, so compile/runtime tests remain to be run.
- The linked endpoint must still apply its usual authorization rules. UI links do not imply access.

## Follow-up
A common metadata/typed relation registry can later unify navigation for all modules (Group, User, Role, DigitalProcesses), while allowing entity-specific labels and view routes.
