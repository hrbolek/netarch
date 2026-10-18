# ZArchitecture 90 — FormDefinition creation authorization and complete test solution

- Included three existing test projects in `SignFakeArchitecture.sln`; run `dotnet test SignFakeArchitecture.sln -c Debug`.
- HTML POST `/digital-processes/FormDefinitionResponse/create-form` catches authorization failures, logs them, returns HTTP 403 with user-readable HTML and link back (rather than unhandled raw exception).
- New `GET /digital-processes/FormDefinitionResponse/create-permission` reports effective `Create` access, target scope UUID and administration guidance (does not grant permissions).
- Authorization target is the seeded RBAC `Group` **Create Form Definitions** (`11000000-0000-0000-0000-000000000001`). `FormDefinitionService.CreateTypedAsync` checks `CreationScopeGuard.RequireEntityAsync<GroupModel>` using this scope, and creation also calls `StageOwnedGroupAsync`/`StageCreateAsync`.
- Bootstrap initializer creates the scope beneath root with `inheritRoles=true`. Bootstrap admin has root Administrator role; RoleType `Administrator` normally gets all operations **only on initial policy creation**. Existing RoleTypeOperation assignments are not overwritten. EffectiveAccess materialization, active dates, scope inheritance, and applicable Deny still matter. Inspect them if the bootstrap actor is denied; do not silently bypass RBAC.
- Remaining endpoints are unchanged. No schema migration.
- Verification: ZIP consistency, solution project paths validated. .NET SDK unavailable in this environment, compilation/test execution NOT performed.
- Added lightweight regression test `FormDefinitionCreationGuidanceTests` verifying the denied HTML identifies the correct operation and scope (full HTTP/DB authorization tests still require a running .NET test environment).
