# Core.Contracts

Dependency-free .NET 10 public contracts shared by persistence, services,
resolvers and UI. This project contains no EF Core packages, database context,
security store, business implementation, host or DI registration.

The physical project path is `src/Core/Contracts/Core.Contracts.csproj`.
The folder expresses its role within Core; its project/assembly name remains
`Core.Contracts`. All project and solution references use this path. The layout
change does not rename any contract or change its namespace, defining assembly
or Base type forwarders.
The reorganized main solutions build successfully; all 317 application/contract
tests pass. Layout and ownership regressions cover the new Contracts path,
entity-owned GroupType assemblies, flat small-project sources, DBLayer folders
and project-cycle freedom. The NuGet audit of 48 solution projects reports no
known vulnerabilities or audit problems.

Existing types retain their original fully qualified names in
`WebAppArchitecture.Base.Data`, `WebAppArchitecture.Base.Services` and
`WebAppArchitecture.Base.Security`.
`Data/`, `DTO/`, `Services/` and `Queries/` are organizational folders, not
namespace migrations. New query contracts use
`WebAppArchitecture.Base.Queries`.

## Extraction inventory

The declarations and their consumer map were inspected before extraction.
The following files were moved without changing their contents, including
documentation, inheritance, constraints, static abstract members, default
implementations and nullability.

| Type | Original path relative to `src/Core/Base/` | New path |
| --- | --- | --- |
| `IEntityDBModel` | `Data/IEntityDBModel.cs` | [Data/IEntityDBModel.cs](Data/IEntityDBModel.cs) |
| `ISecuredEntityDBModel` | `Data/ISecuredEntityDBModel.cs` | [Data/ISecuredEntityDBModel.cs](Data/ISecuredEntityDBModel.cs) |
| `IEntityWithTree` | `Data/IEntityWithTree.cs` | [Data/IEntityWithTree.cs](Data/IEntityWithTree.cs) |
| `IEntityDTOModel` | `Data/IEntityDTOModel.cs` | [DTO/IEntityDTOModel.cs](DTO/IEntityDTOModel.cs) |
| `ISecuredEntityDTOModel` | `Data/ISecuredEntityDTOModel.cs` | [DTO/ISecuredEntityDTOModel.cs](DTO/ISecuredEntityDTOModel.cs) |
| `ICreateDTOModel<TEntity>` | `Data/ICreateDTOModel.cs` | [DTO/ICreateDTOModel.cs](DTO/ICreateDTOModel.cs) |
| `IUpdateDTOModel<TEntity>` | `Data/IUpdateDTOModel.cs` | [DTO/IUpdateDTOModel.cs](DTO/IUpdateDTOModel.cs) |
| `IEntityProjection<TDBModel,TDTO>` | `Data/IEntityProjection.cs` | [DTO/IEntityProjection.cs](DTO/IEntityProjection.cs) |
| `IEntityMapper<TDBModel,TDTO>` | `Data/IEntityMapper.cs` | [DTO/IEntityMapper.cs](DTO/IEntityMapper.cs) |
| `ISecuredEntityService<TDBModel>` | `Services/ISecuredEntityService.cs` | [Services/ISecuredEntityService.cs](Services/ISecuredEntityService.cs) |

`ISecuredEntityService<TDBModel>` requires four minimal public support records, also
moved unchanged from `Base/Services` to [Services/](Services/):
`VectorOrderBy`, `VectorQueryOptions`, `VectorPageInfo` and
`VectorPageResult<T>`. Their existing value defaults, `Empty` singleton and
`HasPrevious` property are preserved. They contain no persistence or business
service calls.

Important retained details:

- Model identity remains lowercase `id`; secured models retain `rbacobject_id`,
  timestamps and actor identifiers with their original types.
- `IEntityDTOModel.visual_label` keeps its default implementation.
- Create/update contracts retain `ToModel` and `ApplyTo`, respectively.
  Updates additionally require the original server-issued `updated_at` token.
- Projection retains its static abstract expression property; mapper retains
  static abstract `FromModel`, including both self-referential DTO constraints.
- Existing service contracts still expose their original `IQueryable<TDTO>`
  methods. This extraction does not redesign or tighten their public API.
- [Base type forwarders](../Base/Properties/ContractTypeForwarders.cs) redirect
  the 14 retained extracted entity/service types and four RBAC types to this assembly.
  Base no longer defines them.
  Source consumers need no namespace changes; binary consumers also need
  `Core.Contracts.dll` alongside the updated Base assembly.

## Consumer map before the move

These are direct source uses, with nested projects considered separately.
All listed projects received an explicit `Core.Contracts` project reference;
existing implementation references were kept because source still uses them.
Other projects continue to consume domain contracts or the existing Base
surface transitively.

| Project | Directly used extracted contracts |
| --- | --- |
| `Base` | All extracted interfaces and vector records |
| `DBLayer` | `IEntityDBModel`, `ISecuredEntityDBModel` |
| `AuthorizationLayer` | Secured models/DTOs, update/projection/mapper/service interfaces and vector records |
| `DigitalProcesses.Models` | Entity/secured model, projection, mapper, secured DTO |
| `DigitalProcesses.Services` | Secured model, projection, base service |
| `DigitalProcesses.Resolvers` | Secured model |
| `DigitalProcesses.Module` | Secured model, base service |
| `Group.Models` | Update, projection, mapper, secured DTO |
| `Membership.Models` | Update, projection, mapper, secured DTO |
| `Role.Models` | Update, projection, mapper, secured DTO |
| `RoleType.Models` | Update, projection, mapper, secured DTO |
| `User.Models` | Update, projection, mapper, secured DTO |
| `Group.Services` | Base service |
| `Membership.Services` | Base service, vector options/page types |
| `Role.Services` | Base service, vector options/page types |
| `RoleType.Services` | Base service |
| `User.Services` | Base service |
| `Group.Resolvers` | Vector page info |
| `User.Resolvers` | Vector page info |
| `Group.UI` | Entity DTO |
| `User.UI` | Entity DTO |
| `DigitalProcesses.Integration.Tests` | Entity/secured model, entity DTO, base service |

`Host.Integration.Tests` also receives a direct reference for the new
type-forwarding compatibility regression. The Host, composition projects and
other Models/Services/Resolvers projects were inspected; their existing
references remain unchanged where they do not name extracted contracts directly.

## Deliberately deferred contracts

These are not silently duplicated or mechanically moved:

| Contract / current location | Reason for leaving it in place |
| --- | --- |
| `ITreeEntityDBModel<TSelf>` / `Base/Data` | Shares a source file and documented path encoding with the concrete `TreePath` helper. The helper remains in Base; separating that API is not required for this extraction. Its pure parent `IEntityWithTree` was moved. |
| `IBaseStore<TModel>` / `Base/Data` | Raw persistence gateway with `IQueryable` and a default throwing query implementation; not part of the new authorized read surface. Technically extractable, but intentionally deferred rather than broadening that surface. |
| `IUserStore`, `IGroupStore`, `IGroupTypeStore`, `IMembershipStore`, `IRoleStore`, `IRoleTypeStore` / `DBLayer` | Their generic arguments are concrete persistence models defined in DBLayer. Moving unchanged would introduce a forbidden DBLayer dependency. |
| `IRbacAccessBindingManager` / `AuthorizationLayer` | Public methods use concrete `DBLayer.RbacAccessBindingModel`. Moving unchanged would introduce a forbidden DBLayer dependency. |
| `ICurrentActorProvider` / `Base/Identity` | Depends on ASP.NET Core `HttpContext`; extracting would add a web framework dependency to the minimal contract library. Its HTTP implementation lives in Hosting/Identity, without changing identity precedence. The redundant `IRbacService` adapter was removed. |
| `IDBLayer` / `DBLayer` | Raw query/staged-write/transaction gateway; deliberately not re-exported as an authorized API. Its interface is technically extractable in a separately scoped persistence-boundary change. |
| `IAuthorizationLayer` / `AuthorizationLayer` | Existing mutation/transaction and raw composable-query boundary; default operation arguments depend on `SecurityOperations`. Moving the complete security substrate is not necessary for the two new read contracts. |
| `ISecurityQueryProvider` / `AuthorizationLayer` | Currently shares its file with the concrete `RbacSecurityQueryProvider` adapter to `IAuthorizationLayer`. Existing implementations and registrations are untouched. |
| `IRbacAuthorizationService`, `ISecurityProjectionService` / `AuthorizationLayer` | Existing RBAC substrate/rebuild APIs are intentionally left together with their current contract families and registrations. The projection interface is technically extractable, but not needed for this minimal foundation. |

No claim is made that every deferred interface is intrinsically impossible to
extract. Some have concrete/web dependencies; others are explicitly deferred
to avoid expanding privileged or unrelated contract families.

## Authorization boundary consolidation

The former SecurityLayer project, namespace, implementation and interface are
now AuthorizationLayer and IAuthorizationLayer. All repository consumers,
both solution formats, test project and scaffolding templates use the new names.
The implementation retains its existing query, controlled-write and transaction
behavior; this is not a replacement authorization engine.

The pure `IRbacAuthorizationService` contract lives in [Security/](Security/), together with
`RbacOperations`, `RbacOperationIds` and `RbacWellKnown`, preserving their full
names. Base forwards these four types. `RbacOperations` is the canonical
operation vocabulary; AuthorizationLayer's existing `SecurityOperations`
constants are compatibility aliases with unchanged values.

The unused legacy RBAC endpoints, their two unimplemented interfaces and eleven
request/record types were removed after checking repository source references
and active endpoint/DI registration. The active Security module is unchanged.
These removed APIs are not retained for external binary compatibility.

HTTP identity implementations and Entra integration are in Hosting/Identity.
Database fake-identity seed data is in DBLayer/Seeding, because DatabaseInitializer
consumes it and must not depend on Hosting. The HTTP actor abstraction remains in
Base/Identity to avoid a cycle. Their existing namespaces are preserved; Base
does not reference Hosting or DBLayer and therefore cannot forward the relocated
HTTP implementations or seed types. External consumers of those implementations,
and of the renamed layer, must update references and rebuild.

No AuthorizedQuery/AuthorizedQueries implementation or DI registration is added.
Validation of this consolidation passed: both solution formats contain the
same projects; all 49 project graphs have valid targets and no cycles.
Core.Contracts remains package/project-reference free, and its four tests pass.
Host tests pass 14/14, DigitalProcesses 190/190, and the final complete
authorization test rerun passes 105/105. The full validation workflow returned
0; its direct/transitive NuGet audit has no known vulnerabilities or audit
problems across 45 solution projects.

## Dependency rules

```text
DBLayer ----------------------+
AuthorizationLayer ----------------+
Models / Services / Resolvers +----> Core.Contracts ----> .NET BCL only
UI / tests -------------------+
Base (including forwarders) --+

Existing implementation links remain in place:
Services -> AuthorizationLayer -> DBLayer -> Base -> Core.Contracts
Resolvers / Host / composition -> existing domain projects
```

- Core.Contracts must not reference Base, DBLayer, AuthorizationLayer, UI, Host,
  domain implementations, EF Core, or ASP.NET Core.
- Implementation projects may reference Core.Contracts; the reverse is forbidden.
- Add only minimal public abstractions and required signature-level value types.
  Do not add data access, RBAC evaluation, DI registrations or service behavior.
- Retain existing full names, nullability, default members and constraints.
  Never duplicate a definition in Base or use `InternalsVisibleTo` to bridge layers.
- This extraction does not remove still-used implementation references. Existing
  Services constructors using `IDBLayer` or `IAuthorizationLayer` remain unchanged.

## Authorized read contracts

### Secured entity services and write contracts

`ISecuredEntityService<TDBModel>` is the single entity-service contract;
the former `IBaseService<TDBModel>` has been removed. Its implementation is
`AuthorizationLayer.SecuredEntityService<TDBModel>`.
`IRbacAuthorizationService` is the pure actor/object/operation decision contract,
implemented by `RbacAuthorizationService`; it includes permitted-operation lookup.
It replaces the duplicate implementation-specific and HTTP RBAC adapters.
`SecurityPermissionService` remains the permission-administration projection.
External source/binary consumers of the removed names must migrate and rebuild.

`VectorAsync` remains an unpaged collection read unless an explicit limit is
supplied. `VectorPageAsync` returns a bounded page (default 25) and continuation
metadata using an additional probe row. They are intentionally not collapsed
into one method: that would silently truncate existing collection readers or
change the cost/meaning of paging.

The generic `/list` endpoint always checks the distinct `List` permission; a
caller cannot choose its RBAC operation in the query string. Related-entity
creation pickers use the dedicated `/GroupResponse/create-targets` endpoint,
which always checks `Create`. A picker therefore shows exactly the scopes in
which the current actor may create the related entity.

`IUpdateDTOModel<TEntity>.updated_at` is a non-nullable original timestamp, not
a requested audit value. Built-in JSON requests require its presence, and HTML
forms round-trip it in a hidden field. Missing/invalid tokens return 400; stale
tokens and SQL concurrency conflicts return 409. `ApplyTo` changes business
fields only. The authorized persistence pipeline generates UTC, PostgreSQL-
precision timestamps and the actor identity, and retains original creation
metadata. EF includes the original timestamp in the UPDATE predicate, including
models added by derived contexts. Privileged raw DBLayer import/seed APIs remain
explicitly separate. Failed owned transactions clear their tracked changes.

Static resolver classes do not inherit. Standard Core entity routes now bind closed
generic `BaseResolvers` methods directly; domain-specific create/form/relation
handlers remain in their entity resolvers. The forwarding-only GroupType resolver
class was removed; its endpoint registration project remains.
Routes and representations are unchanged.

### Future authorized read surface

[IAuthorizedQuery<TDBModel>](Queries/IAuthorizedQuery.cs) and
[IAuthorizedQueries](Queries/IAuthorizedQueries.cs) are interfaces only.

`Read<TDBModel>()` denotes a source restricted by the Read permission of the
effective actor supplied by the trusted context. It accepts no caller-selected
user id. Missing identity must not become unrestricted access.

The query supports expression-based `Where`, ordering, paging and async
materialization/count/exists operations with cancellation. It does not inherit
`IQueryable`, expose a provider/context/set, accept a replacement source, or
provide bulk mutation methods. `Select`, `WithOne` and `WithMany` are deferred.

**Next step:** implement these interfaces in AuthorizationLayer using the existing
trusted-identity and RBAC pipeline, preserving authorization during composition.
Only that future step should register the implementation in DI or migrate
callers. No `AuthorizedQuery<TDBModel>` or `AuthorizedQueries` class, fallback
implementation, or new authorization registration is introduced here.

## Validation

```bash
dotnet restore SignFakeArchitecture.sln
dotnet build src/Core/Contracts/Core.Contracts.csproj -c Release --no-restore
dotnet build SignFakeArchitecture.sln -c Release --no-restore
dotnet test SignFakeArchitecture.sln -c Release --no-build --no-restore
dotnet test tests/Core/Core.Contracts.Tests/Core.Contracts.Tests.csproj -c Release
```

The independent contract test project references only Core.Contracts plus the
existing xUnit tooling. It tests the static generic contracts, default DTO
implementation, safe query method surface, nullability, constraints and absence
of implementation/framework dependencies. The Host regression checks Base type
forwarding and absence of duplicate Base definitions.

An extraction-time comparison of all 15 moved types against the original Base
binary checked 209 signature lines, including methods, generic constraints,
nullability, default values and public constructors. The only intended identity
change is the defining assembly; the namespace and signatures match. Source
inspection found one definition per moved/new type and no project cycles;
Core.Contracts has neither project references nor NuGet package dependencies.

Observed validation results immediately after extraction, before correcting
the separately confirmed UI regression expectations:

| Check | Result |
| --- | --- |
| Main solution restore | Passed |
| Standalone Core.Contracts Release build | Passed |
| Main solution Release build | Passed; initial build 26 warnings, workflow rerun 4 AD0001 analyzer warnings; no compiler errors |
| Retained standalone ArchitectureInspector solution Release build | Passed, 0 warnings / 0 errors |
| Core.Contracts.Tests | 4 passed / 0 failed |
| Core.AuthorizationLayer.Tests | 104 passed / 0 failed |
| Host.Integration.Tests | 12 passed / 1 failed |
| DigitalProcesses.Integration.Tests | 181 passed / 7 failed |
| Direct/transitive NuGet vulnerability audit | No known vulnerabilities |

The 8 application failures match the pre-extraction baseline: root redirect
expectation, form-designer permission text and form editor markup. Existing
expectations and application behavior were not changed to conceal them.
The dependency and Base forwarding regressions also pass.
At that point `dotnet test` exited nonzero for the full solution, while
the new focused contract tests are green.
The repository validation script was also run: it completed build, tests and
the NuGet audit, and returned 1 because of those same 8 failing application
tests. Its remaining build warnings are ASP.NET RouteHandlerAnalyzer exceptions
in Host, not compilation errors. The final audit covers all 45 solution projects
and reports neither vulnerable packages nor audit problems.

The subsequent UI regression correction is separate from contract extraction:
the Host root must show application cards, the insertion wizard replaces the
standalone Library card, and permissions belong only in the version LargeCard.
Whole-form AI design is not exposed in subtree editors. Tests now assert those
confirmed behaviors, including a single permission panel in form/workflow
designers, rather than restoring the obsolete UI. See the
[root README](../../../README.md#home-and-designer-ui-regression-expectations).
After that correction, the repository validation script returns 0: all 311
tests pass (contracts 4, security 104, Host 13, DigitalProcesses 190), the Release
build has no errors, and the 45-project direct/transitive NuGet audit has no
known vulnerabilities or audit problems.

### Project files changed

Created:

- `src/Core/Contracts/Core.Contracts.csproj`
- `tests/Core/Core.Contracts.Tests/Core.Contracts.Tests.csproj`

Existing project files receiving a direct reference (none removed):

- `src/Core/Base/Base.csproj`
- `src/Core/DBLayer/DBLayer.csproj`
- `src/Core/AuthorizationLayer/AuthorizationLayer.csproj`
- `src/Apps/DigitalProcesses/DigitalProcesses.Models/DigitalProcesses.Models.csproj`
- `src/Apps/DigitalProcesses/DigitalProcesses.Services/DigitalProcesses.Services.csproj`
- `src/Apps/DigitalProcesses/DigitalProcesses.Resolvers/DigitalProcesses.Resolvers.csproj`
- `src/Apps/DigitalProcesses/DigitalProcesses.Module/DigitalProcesses.Module.csproj`
- `src/Core/Group/Group.Models/Group.Models.csproj`
- `src/Core/Group/Group.Services/Group.Services.csproj`
- `src/Core/Group/Group.Resolvers/Group.Resolvers.csproj`
- `src/Core/Group/Group.UI/Group.UI.csproj`
- `src/Core/Membership/Membership.Models/Membership.Models.csproj`
- `src/Core/Membership/Membership.Services/Membership.Services.csproj`
- `src/Core/Role/Role.Models/Role.Models.csproj`
- `src/Core/Role/Role.Services/Role.Services.csproj`
- `src/Core/RoleType/RoleType.Models/RoleType.Models.csproj`
- `src/Core/RoleType/RoleType.Services/RoleType.Services.csproj`
- `src/Core/User/User.Models/User.Models.csproj`
- `src/Core/User/User.Services/User.Services.csproj`
- `src/Core/User/User.Resolvers/User.Resolvers.csproj`
- `src/Core/User/User.UI/User.UI.csproj`
- `tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj`
- `tests/Core/Host.Integration.Tests/Host.Integration.Tests.csproj`

Both root solution formats include Core.Contracts and its standalone tests.
