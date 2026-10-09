# Architecture Update 54

- Creating a FormDefinition now atomically creates draft FormVersion 1.
- FormDefinition detail uses a dedicated read model and lists all readable versions with Designer/Test actions.
- Standard FormNodeTypes are bootstrapped by DigitalProcesses.Module: Section, Single-line text, Multi-line text, Number, Date, Checkbox and Select.
- Added TextAreaRenderer and SelectRenderer; designer Add Node shows the standard types first.
- Added `IFormDataBackend` registered through DI with explicit `PreprocessAsync`, `FetchAsync`, `PostprocessAsync`, and `ExecuteAsync` phases.
- The Digital Processes data backend is one GraphQL-like endpoint configured by `DigitalProcesses:DataBackend:Endpoint`. No per-form JSONata pre/post transforms are introduced.
- Backend parameter bindings identify qualified FormNode keys. An exact key resolves a scalar; binding a section prefix (for example `person`) assembles an object from `person.*`. Runtime-only parameters such as select search text use `FormBackendParameterSource.RuntimeValue`.

## Backend parameter binding example

```csharp
new FormBackendOperationRequest(
    query,
    new[]
    {
        new FormBackendParameterBinding("personId", FormBackendParameterSource.FormValue, "person.id"),
        new FormBackendParameterBinding("person", FormBackendParameterSource.FormValue, "person"),
        new FormBackendParameterBinding("search", FormBackendParameterSource.RuntimeValue, "search")
    });
```

`person.id` resolves one scalar. `person` resolves an object assembled from all current values under `person.*`. `search` comes from runtime interaction rather than persisted form state.
