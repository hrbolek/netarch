# Resolver security boundary framework

These tests exercise the existing Host and service architecture. They do not
replace RBAC with a test implementation and do not change application security
semantics.

## Three independent forms of evidence

1. **HTTP delegation contracts** use `ResolverBoundaryHost`, a
   `WebApplicationFactory<Program>`. Production `Program`, module routing,
   middleware, trusted-proxy identity integration, JSON binding, and
   representation endpoint filters are unchanged. Only application background
   initialization is removed. Registered service interfaces are replaced by
   strict `DispatchProxy` spies; resolving persistence/context/store or
   `IAuthorizationLayer` directly fails and is recorded. No relational server is
   needed for this isolated host.
2. **Service enforcement** in
   `../../../Core.AuthorizationLayer.Tests/SecuredServiceBoundaryTests.cs`
   calls real `UserService` and `MembershipService` without routing or
   resolvers, using real `AuthorizationLayer`, RBAC queries/projections,
   `DBLayer`, and a disposable PostgreSQL database. Identity-only HTTP context
   is supplied because the current authorization contract uses
   `IHttpContextAccessor`. A projection fault after `SaveChanges` verifies
   rollback from a separate EF context/connection, not merely tracked state.
3. **Deterministic dependency rules** inspect project references as XML and
   resolved compiled IL members/types. They cover resolver assemblies, actual
   registered handler types, compiler-generated async/nested code, and generic
   `BaseResolvers`. Tests detect forbidden persistence fields, parameters,
   return types, member access, construction, and generic service location.
   Negative fixtures prove that the inspector recognizes a raw `IDBLayer`
   query, `GetRequiredService<CoreDbContext>()`, and direct ADO.NET/Npgsql
   connections/commands that would bypass EF and DBLayer entirely.

A passing spy test proves only the tested delegation/transport behavior.
Service tests provide separate evidence for the actual policy and persistence
effects. Neither constitutes a universal proof of authorization correctness.

## Strict fake contract

`StrictServiceSpy` supports interfaces already registered by production DI.
Configure an ordered method expectation, exact generic arguments, argument
assertions, and a typed result or explicit exception. It records method,
arguments, effective actor and request path. Calls are unexpected by default;
there is no implicit empty collection, `false`, successful task or RBAC
fallback. Call `host.Verify()` after every scenario: it rejects unexpected
calls, unused expectations, swallowed argument assertions and forbidden
dependency resolution, even when an endpoint catches an exception.
Expectations match resolved interface methods, not just method-name strings;
use `ExpectOverload` for an unambiguous overloaded signature. A different
overload fails even if its method name and DTO generic arguments match.

```csharp
host.Spy<IUserService>().Expect("GetByIdAsync", call =>
{
    Assert.Equal(expectedId, call.Arguments[0]);
    Assert.Equal(expectedActor, call.Actor);
    return Task.FromResult<UserResponse?>(null);
}, typeof(UserResponse));
```

The spies are not another policy engine. A configured permission result tests
resolver orchestration only. Services implemented as sealed concrete classes
(notably Events) are not silently replaced with interface proxies. Their HTTP
actor-gate tests use real service instances with strict authorization/database
dependency spies and a providerless, disconnected catalog context. These prove
early rejection only, not authorized Events behavior. Existing
`EventsArchitectureTests` also check selected handler actor gates directly.

## Covered resolver contracts

- User, Group, Role and Membership: list/read/edit/link, typed contained create,
  update token/identity forwarding, fixed update/delete operations,
  allow/deny decisions, early exits, and service failures.
- Every production route forwarding directly to generic `BaseResolvers.List`
  (including DigitalProcesses wrappers): fixed `Read` and rejection of
  caller-chosen `operation`; discovery uses resolved compiled calls, not route
  naming.
- Group create-target picker: fixed `Create`.
- User membership vector: parent-bound predicate and fixed `Read`.
- DigitalProcesses form creation, permission query, and next-version command:
  designated service, trusted identity, redirects and error behavior.
- DigitalProcesses runtime start/transition and AI-design auxiliary results:
  designated service, identity binding, fail-closed exceptions and preservation
  of explicit 200/401/403/404/409 outcomes without external AI calls.
- Events list/type-list/view/children: anonymous HTTP requests cannot reach
  concrete service persistence/authorization dependencies.
- Infrastructure self-tests prove strict-spy and persistence-guard failures are
  observable rather than hidden by HTTP error translation.

The assertion that denied operations make no unintended service calls is
bounded by each scenario's configured expectations. For example, delete is
allowed to perform its normal secured read preflight, but may not call mutation
after denial.

## Endpoint inventory and coverage report

`EndpointSecurityInventoryTests` enumerates all actual `RouteEndpoint`
method/route pairs in the current Host build and writes:

- `artifacts/security-boundaries/endpoint-security.json`
- `artifacts/security-boundaries/endpoint-security.md`

Each entry contains route, HTTP method, name, actual handler, ASP.NET
authorization metadata, service parameter dependencies, verified generic
operation expectations, explicitly mapped existing security test evidence,
and declared strict contract test references.
References are checked against real routes and test methods; removed routes or
stale test names fail validation. Newly added routes automatically appear.
`UNCOVERED` means no declared strict HTTP contract test, not necessarily no
existing integration test. Existing evidence entries identify limited known
cases and are not an exhaustive cross-project test coverage analysis.
Existing broader suites include
`GroupListOperationTests`, `SecuredUpdateTests`,
`DigitalProcessesAuthorizationHttpTests`,
`DigitalProcessesCommandAtomicityTests`, and the DigitalProcesses lifecycle,
scope and atomicity suites. Their source and TRX results must be consulted for
scenario-specific evidence.

Custom service-owned policies without verified operation mapping are marked
`UNKNOWN`. Do not infer a policy from a URL or method name. Absence of ASP.NET
policy metadata does not establish a public endpoint: much of the existing
application enforces actor/RBAC checks in handlers and services. Conversely,
presence of metadata does not prove service enforcement.

The inventory is build-specific: Debug includes optional Jsonata diagnostics;
Release does not. Middleware-only Swagger endpoints and static file serving
are not `RouteEndpoint` entries and are explicitly outside this report.
Declared tests are not execution results; correlate them with TRX.

## Existing reference exceptions

`ResolverDependencyTests.ContractReferences` permits only the existing direct
project references needed for shared database-model types or operation
constants:

| Resolver project | Referenced project | Scope |
|---|---|---|
| GroupType.Resolvers | DBLayer | Model types used by generic endpoints |
| Security.Resolvers | AuthorizationLayer | Existing security contract/constants |
| Events.Resolvers | DBLayer, AuthorizationLayer | Existing model/operation contracts |
| DigitalProcesses.Resolvers | DBLayer | Model types used by generic endpoints |

These are reference-shape exceptions, **not permission to query/mutate
persistence**. No persistence-member exception exists. A new reference fails
the project check; removing an obsolete reference requires removing its stale
exception. Resolver model signatures remain permitted because existing shared
models live in the persistence assembly. The tests do not redefine that
existing architecture or move application code.

The existing DigitalProcesses handler-naming test explicitly permits the exact
`EventsUiAssets.Get` method and verifies its `Events.UI` ownership. This serves
embedded UI assets, not protected entity data; it must not be forced back into
Resolvers merely to satisfy a class-name suffix.

## Known response and coverage gaps

- The Host does not register an authentication handler for generic
  `Results.Forbid()`. A false DTO permission decision currently ends as HTTP
  **500**, not the desired 403. Characterization assertions name this gap;
  mutation is still prevented.
- Unhandled `UnauthorizedAccessException` in generic/API create paths likewise
  currently produces **500**. DigitalProcesses form-create and create-permission
  handlers explicitly translate their denial to **403**, which is tested.
- Correct fail-closed behavior is not equivalent to correct HTTP denial
  translation. These tests preserve observed semantics; correcting translation
  requires a separate behavior change.
- Trusted EasyAuth/actor headers are an existing deployment assumption. The
  proxy must strip/overwrite client-supplied identity headers. These tests
  prove query/body identity cannot override the effective actor; they do not
  prove the external proxy configuration or token authenticity.
- Not every domain command, HTML rendering path, collection relationship,
  self-service/import exception, or custom policy has a strict scenario yet.
  The report highlights those paths rather than granting blanket approval.
- Static checks do not prove absence of reflection/dynamic invocation,
  external side effects or a bypass hidden in another layer. Existing
  downstream architecture/service tests remain necessary.

## Running locally and in CI

```sh
# No database required: production HTTP routing with strict service isolation.
dotnet test tests/Core/Host.Integration.Tests/Host.Integration.Tests.csproj \
  --filter FullyQualifiedName~SecurityBoundary

# Requires the isolated postgres_test server configured by the existing fixture.
dotnet test tests/Core.AuthorizationLayer.Tests/Core.AuthorizationLayer.Tests.csproj \
  --filter FullyQualifiedName~SecuredServiceBoundaryTests

# Full repository Release build/tests and direct/transitive NuGet audit.
./tools/audit-architecture.sh
```

The two suites are in existing solution test projects and require no separate
runner or AI service. The existing CI audit job runs them through `dotnet test`,
starts a disposable PostgreSQL service named `postgres_test`, and uploads TRX,
NuGet audit and endpoint coverage artifacts. Test-only trust authentication is
confined to that ephemeral CI database; it is not application configuration.
The audit serializes test projects because the relational suites create/drop
databases on one shared test server; this avoids cross-project cleanup
contention without disabling any tests or increasing failure timeouts.
