# Facilities

Facilities adds physical locations and Event reservations as an independently
registered application module. It uses the existing shared Host EF context;
it does not introduce a second database unit of work, an alternative RBAC
implementation, or shared Core contracts.

## Ownership and dependency direction

- `Facilities.Models`: physical facility/tree and type contracts, reservation
  contracts, DTO projections and EF mappings.
- `Facilities.Services`: secured tree/CRUD operations and reservation commands.
- `Facilities.UI`: Razor pages, cards and forms.
- `Facilities.Resolvers`: HTTP input/output, trusted actor integration and
  representation metadata; no direct persistence access.
- `FacilitiesModule`: DI, EF model contribution, schema upgrade, navigation
  and endpoint registration.

The Event association is owned by Facilities. Dependencies may point from
Facilities to existing Events contracts/services, never from Events back to
Facilities. Multiple reservation records support multiple locations per Event
and repeated use of a location over time.

Facility implements the existing materialized-path tree contract and reuses
`TreeSecuredService<FacilityModel>`. FacilityType is deliberately flat:
`CanContainChildren` is a hard gate, while `AllowedChildTypeIds` optionally
restricts compatible child types. An empty allowed set means unrestricted
types, not a hardcoded campus/building/floor hierarchy.

## Security contract

The requester approved the initial policy in
[the architecture decision](../../../docs/architecture/decisions/facilities-reservations.md).

Physical containment is not security-group containment. Each Facility owns an
RBAC object in a separate Group-based permission tree. The requester clarified
that a move must copy the existing permission subtree, including roles and
memberships, beneath the destination's permission group and recalculate
EffectiveAccess. Merely changing the physical path or refreshing unchanged
scopes is insufficient. User-supplied audit values and actor fields must not
become persisted audit identity.

Reservation authorization is enforced by services, including calls made
without HTTP:

| Operation | Required existing RBAC operations |
|---|---|
| Request | Read Facility; Update Event; Create explicit reservation scope |
| Confirm/reject/cancel | Update Facility, Event and Reservation |
| Related reads | Read Facility, Event and Reservation |

This intentionally conservative initial policy is not a new global Core rule.
Resolvers cannot choose a weaker operation through HTTP input.

Child creation requires Create on its parent's owned permission Group. Move
requires `move` on the existing root scope and Create on the target's owned
Group. The operation catalog entry is provisioned without granting it to
existing roles. Administrators must explicitly include it in the appropriate
role-type operation policy.

The permission copy is a narrow command-level audited-persistence exception,
not an alternative CRUD architecture. It preserves original Groups/grants and
creates fresh Group/role/membership/deny identities. Unsupported bindings and
external deputy references are rejected. Old-tree cleanup is not implicit.
See [the service policy](Facilities.Services/README.md) for details.

Because the two trees are separate, Move validates the association boundary:
every moved physical descendant must have a copied scope, and the copied
permission subtree must not map to Facilities outside the physical subtree.
Inconsistent associations are rejected atomically rather than silently
changing unrelated Facilities.

Existing reservations keep their independent explicit scopes when a Facility
moves. A newly authorized Facility reader does not automatically gain access
to historical reservation/Event details; the three-way Read policy still
applies. Hidden confirmed bookings still block conflicting confirmations.

## HTTP and Razor

The Host mounts the module at `/facilities`.

| Entity prefix | Operations |
|---|---|
| `/FacilityResponse` | GET `list`, `tree`, `view/{id}`, `children/{id}`, `ancestors/{id}`, `descendants/{id}`; POST `create`, `move/{id}`; PUT `update`, `update/{id}`; DELETE `delete/{id}` |
| `/FacilityTypeResponse` | GET `list`, `view/{id}`; POST `create`; PUT `update`, `update/{id}`; DELETE `delete/{id}` |
| `/FacilityReservationResponse` | POST `request`, `confirm/{id}`, `reject/{id}`, `cancel/{id}`; GET `by-facility/{facilityId}`, `by-event/{eventId}`, `facilities-by-event/{eventId}`, `availability/{facilityId}`, `occupancy/{facilityId}` |

Facility navigation exposes list/tree/detail/create/edit pages, FacilityType
administration and reservation forms/overviews. HTML belongs to the Razor UI
project. Lists negotiate JSON/HTML through existing representation helpers;
API-first operations return DTOs and documented HTTP errors.

The `facilities-by-event` query returns distinct facilities from readable
reservation history across all statuses. It is not limited to confirmed or
currently occupied space.

Updates, moves, deletes and reservation transitions carry the latest
server-issued `updated_at` token. Ordinary Facility update DTOs do not expose
ParentId, path or RBAC scope; parent changes use only the dedicated Move command.
Move requires a destination Facility in the initial implementation.

## Reservations and Event lifecycle

Reservation intervals are half-open `[start, end)`. Adjacent reservations do
not overlap. Only confirmed reservations occupy space; pending, rejected and
cancelled requests do not represent current occupancy. Occupancy is derived,
not stored on Facility.

Conflicts are evaluated for the same Facility identifier. Booking a container
does not implicitly book its physical descendants; cross-level exclusivity
would require a separate domain rule.

All domain writes share a PostgreSQL transaction-scoped advisory lock;
reservation writes also take a per-facility lock before checking conflicts.
This serializes concurrent confirmations and related type/active/scope changes
through commit. Permission copying additionally locks its source security
tables so Core writes cannot produce an inconsistent clone. These deliberate
coarse locks prioritize correctness for the initial implementation; they can
limit write throughput and should be measured before any finer-grained design.

Facilities persistence and schema upgrades require PostgreSQL. The bootstrap
is additive and uses the composed Host model for empty databases. Reservation
history restricts Facility/Event deletion rather than silently cascading.

Automatic cancellation or rescheduling when an Event changes is **not
approved**. Operators must explicitly cancel/rebook reservations. No Events
contract or runtime behavior is silently modified to introduce lifecycle hooks.
The decision record documents coordinator and transactional-notification
alternatives for future approval.

## Development

The module is optional agent-assisted development, not agent-dependent runtime
infrastructure. Standard `dotnet build`, `dotnet test`, and
`tools/audit-architecture.sh` remain the validation entry points. The endpoint
security inventory generated by Host tests includes the installed Facilities
routes and must distinguish declared scenarios from authorization guarantees.

```sh
dotnet test tests/Apps/Facilities.Integration.Tests/Facilities.Integration.Tests.csproj --configuration Release
dotnet test tests/Core/Host.Integration.Tests/Host.Integration.Tests.csproj --configuration Release --filter FullyQualifiedName~FacilitiesResolverBoundaryTests
./tools/audit-architecture.sh
```

Relational tests use the existing isolated `postgres_test` fixture, never a
development database. Resolver tests use production registrations with strict
service spies and a persistence-resolution guard. Passing spy tests establish
delegation evidence, not a substitute for real service authorization tests.

## Agent framework evaluation

Specialists actually invoked for this implementation:

| Role | Contribution |
|---|---|
| Entity Developer | Domain models/DTOs, secured services, schema bootstrap and permission-command integration |
| Generic Architecture Engineer | Existing tree/projection API review; domain-local permission-tree copy helper where existing generic CRUD was insufficient |
| API Services Engineer | Thin endpoint registration, representations, OpenAPI metadata and module composition |
| Razor UI Engineer | Shared-component-based pages, cards, forms and tree browser |
| Test Quality Engineer | PostgreSQL service, permission-copy and reservation-concurrency regressions |
| Security Architect | Independent authorization/concurrency review; identified and rechecked generic related-read filtering and non-reservable reassignment fixes |
| Architecture Guardian | Independent read-only architecture/contract review; identified stale integration contracts and API metadata |

The orchestrator handled approved decisions, Host/solution integration, strict
HTTP spy tests, endpoint inventory, documentation and aggregate validation.
Parallel implementation was separated by owned projects; the permission-tree
policy change was coordinated explicitly rather than inferred from physical
parentage. No agent tooling is a runtime dependency or a mandatory CI step.

## Delivery validation

The final serialized `./tools/audit-architecture.sh` completed with exit code
0 against the completed implementation:

| Check | Result |
|---|---|
| Main solution Release build | Passed; 0 errors, 2 SDK `AD0001` RouteHandlerAnalyzer warnings |
| Core.AuthorizationLayer.Tests | 113 passed, 0 failed, 0 skipped |
| Host.Integration.Tests | 143 passed, 0 failed, 0 skipped |
| DigitalProcesses.Integration.Tests | 190 passed, 0 failed, 0 skipped |
| Core.Contracts.Tests | 4 passed, 0 failed, 0 skipped |
| Facilities.Integration.Tests | 26 passed, 0 failed, 0 skipped |
| Total | 476 passed, 0 failed, 0 skipped |
| Direct/transitive NuGet vulnerability audit | 55 projects; 0 reported vulnerabilities; no audit problems |

The alternative `SignFakeArchitecture.slnx` Release build also passed. Its
earlier full compilation reported the two SDK analyzer warnings and two
existing `xUnit2013` warnings in DigitalProcesses tests. No analyzer or security
check was disabled. No package versions or shared Core/Events production
contracts were changed for this module.

The [relational suite](../../../tests/Apps/Facilities.Integration.Tests/FacilitiesRelationalTests.cs)
exercises actual PostgreSQL services, authorization, permission-copy rollback,
tree paths, schema upgrade and concurrent reservation behavior.
The [HTTP suite](../../../tests/Core/Host.Integration.Tests/SecurityBoundary/FacilitiesResolverBoundaryTests.cs)
uses production endpoints with strict service spies, separately from those
service enforcement tests.

The generated [endpoint inventory](../../../artifacts/security-boundaries/endpoint-security.md)
contains 327 method/route pairs. All 32 Facilities pairs have explicit operation
policies and declared contract scenarios. Repository-wide, 236 pairs still lack
declared strict contract tests and 242 have an unknown/inferred-unavailable RBAC
operation; these existing gaps are not claimed as covered by this delivery.
Declarations and passing spies do not prove universal authorization correctness.
The [NuGet report](../../../artifacts/nuget-audit.json) records the package audit,
not an architectural guarantee.

Independent Architecture Guardian closure found no verified unresolved
findings in its final targeted scope (updated relational contracts, API policy
metadata and solution nesting). Security Architect rechecked the reported
authorization fixes. Neither review substitutes for executed tests or covers
future changes.
