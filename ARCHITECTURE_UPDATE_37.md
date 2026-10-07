# Architecture Update 37

## RoleAssignmentDialog fragment parameter

`ApplyViewModel<TView,TModel>` always passes both `Model` and `IsFragment` to Razor components.
`RoleAssignmentDialog` was the only component used through `ApplyViewModel` that did not declare the
`IsFragment` parameter, causing runtime rendering to fail.

Added:

```csharp
[Parameter] public bool IsFragment { get; set; }
```

A static audit of Razor components used through `ApplyViewModel` / `ApplyJsonModel` found no other
missing `IsFragment` parameter.
