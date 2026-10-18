# Architecture Update 26

## Fixes

`DigitalProcessResolvers.cs` now explicitly imports:

- `Microsoft.AspNetCore.Builder`
- `WebAppArchitecture.Base.Endpoints`

These are required for `MapGroup`/route builder extensions and `ApplyViewModel` / `ApplyJsonModel`.

## Source tree restructuring

The source tree is now grouped into three primary areas:

```text
src/
├── Apps/
│   └── DigitalProcesses/
├── Core/
│   ├── Base/
│   ├── DBLayer/
│   ├── SecurityLayer/
│   ├── User/
│   ├── Group/
│   ├── Membership/
│   ├── Role/
│   ├── RoleType/
│   └── Security/
└── Host/
```

All project references, solution paths and Docker restore-copy paths were updated.

## DigitalProcesses service split

The former monolithic `DigitalProcessServices.cs` was removed. Services are now split by domain and by class:

```text
DigitalProcesses.Services/Services/
├── Common/
│   └── MaterializedPath.cs
├── Forms/
│   ├── FormDefinitionService.cs
│   ├── FormVersionService.cs
│   ├── FormNodeTypeService.cs
│   └── FormNodeService.cs
├── Workflow/
│   ├── WorkflowDefinitionService.cs
│   ├── WorkflowVersionService.cs
│   ├── WorkflowStateService.cs
│   └── WorkflowTransitionService.cs
├── Process/
│   ├── ProcessDefinitionService.cs
│   └── ProcessDefinitionVersionService.cs
├── Runtime/
│   ├── RequestService.cs
│   ├── RequestTransitionHistoryService.cs
│   ├── SubmissionService.cs
│   └── SubmissionNodeService.cs
└── Integration/
    └── IDigitalProcessIntegration.cs
```

Namespaces are intentionally unchanged (`WebAppArchitecture.DigitalProcesses.Services`) so the split does not create artificial API boundaries.

## Form renderer baseline

Update 25 contained only the renderer infrastructure:

- `IFormNodeRendererRegistry`
- `FormNodeRendererRegistry`
- `FormNodeRenderer.razor` using `DynamicComponent`

Update 26 adds an initial concrete renderer set under:

```text
src/Apps/DigitalProcesses/DigitalProcesses.UI/FormNode/Renderers/
```

Registered renderer keys:

- `section`
- `input.text`
- `input.number`
- `input.date`
- `input.checkbox`
- `action.button`

Registration is centralized in:

```text
DigitalProcesses.UI/FormNode/Renderers/DigitalProcessRendererRegistration.cs
```

and Host calls:

```csharp
builder.Services.AddDigitalProcessRenderers();
```

A new renderer should be added as a Razor component in `DigitalProcesses.UI/FormNode/Renderers/` and registered under a stable `renderer_key`. The database `FormNodeType.renderer_key` remains the indirection between low-code metadata and the actual Razor component.

The current basic renderers are intentionally only visual primitives. In particular `action.button` does not yet encode a GraphQL/JSONata execution endpoint, and dynamic dropdown/data-source renderers still need the runtime action/data-source layer to be wired into the form renderer.

## Security fast path

`SecurityLayer.cs` content is unchanged from Update 25; only its physical path moved under `src/Core/SecurityLayer/`.
