# Architecture Update 46

Fixes the LINQ query-expression build error introduced in Update 45.

## Fix

In both `RoleService` and `MembershipService`, the range variable in secured joins was named `group`:

```csharp
join group in groups ...
```

`group` is a contextual keyword in C# query syntax, so this is invalid. The range variable is now named `grp` and all projections were updated accordingly.

Affected files:

- `src/Core/Role/Role.Services/RoleService.cs`
- `src/Core/Membership/Membership.Services/MembershipService.cs`

No authorization semantics or vector/paging behavior were changed.
