# ARCHITECTURE_UPDATE_67

- `FormVersionService.CreateContainedAsync`: a new draft version clones the latest older version's entire FormNode tree, with new node IDs, rewritten materialized paths, parent IDs, copied configurations and inherited definition security scope.
- `WorkflowVersionService.CreateContainedAsync`: a new draft clones states, transitions (with remapped state IDs), and state access rules in one transaction.
- `ProcessDefinitionVersionService.CloneLatestAsync`: a process draft clones references to its previous form and workflow versions, which remain independent shared, versioned definitions.
- Draft-only deletion is available for FormVersion, WorkflowVersion, and ProcessDefinitionVersion; publication and external references block deletion. Cascaded dependent deletion occurs in one transaction.
- Details of FormDefinition, WorkflowDefinition, and ProcessDefinition include draft deletion and copy actions.
- Existing creation operations and URLs are preserved; `new-version` means clone when there is an earlier version. First versions are created empty.

## Limitations
- Compilation and integration tests could not be executed because .NET SDK is unavailable here.
- RBAC may prevent copying when not all source child records are readable; service follows readable entity queries.
- Process versions intentionally keep form/workflow version references, rather than duplicating the definitions they reference.
