# Facilities service policy

## Approved authorization

Reservation operations use existing RBAC operations. Facility moves introduce
the dedicated `move` catalog operation; provisioning does not grant it to roles.

| Operation | Required authorization |
|---|---|
| Request reservation | Read Facility; Update Event; Create on the explicitly selected, existing reservation scope |
| Confirm, reject, cancel | Update Facility; Update Event; Update Reservation |
| Read reservations (including occupancy) | Read Reservation; Read Facility; Read Event |

Read is independent of Update. Request does not additionally require Event
Read, and transitions do not additionally require Facility/Event Read.
Generic reservation Read/List queries and projections apply the three-entity
Read policy before materialization. Commands return their resulting response
DTO directly after the authorized write.

Each Facility receives a distinct Group-based RBAC object. Root creation
requires Create on the existing group selected by `RbacObjectId`; child creation
requires Create on the physical parent's owned group and creates its owned group
beneath it. For children, `RbacObjectId` is not used to override that placement.

Moves require the dedicated literal `move` operation on the source owned group
and Create on the destination owned group. A non-null physical destination is
required. The permission helper copies the entire source Group subtree, roles,
memberships, and deny rules beneath the destination; source objects remain
untouched. Facilities using any copied scope are rebound, physical descendant
paths are maintained, and EffectiveAccess is synchronized within the transaction.
Reservations retain their independent explicit scopes.

Moves reject access bindings touching the permission subtree and external or
missing deputy dependencies rather than silently losing their semantics.
The `move` operation is provisioned idempotently without assigning it to any role.

There is no automatic reservation mutation when an Event is cancelled or
rescheduled. Lifecycle coordination remains a proposal; Events is unchanged.
Event foreign keys use restrictive deletion to retain reservation history.

## Type compatibility

Facilities require nonempty `Code` and `Name`. Optional `Capacity` must be
nonnegative when supplied. Code is searchable but is not required to be globally
unique. Facility request/response constructors append `Code` and `Capacity`;
omitting Code remains source-compatible but create/update validation rejects it.
The PostgreSQL additive upgrade backfills legacy missing codes from each row's
stable UUID and adds nullable Capacity without changing audit/concurrency fields.

`CanContainChildren = false` denies every child, regardless of the allowed-child
relationship. When true, an empty allowed-child set means unrestricted types;
a nonempty set permits only the listed child types. These rules apply to
creation, moves, facility type changes, and compatibility configuration edits.
Configuration changes that invalidate existing physical relationships fail
transactionally rather than silently removing children.

## Reservations and concurrency

Facilities schema bootstrap and mutation commands require PostgreSQL.
Other database providers are explicitly rejected; no SQLite fallback is provided.

Only confirmed reservations occupy a facility. Requests may overlap.
Conflict checking uses half-open intervals `[start,end)`; adjacent intervals
do not conflict. Request and confirmation require an active facility with a
reservable type, a nonempty interval, and bounds within the current Event.

Availability returns only a boolean to a Facility reader; it does not return
reservation identities or Event details. Its internal conflict probe includes
hidden confirmed reservations so hidden bookings cannot cause a false free
result. Occupancy and reservation lists apply the full three-entity Read policy.

All Facilities writes use the same PostgreSQL transaction advisory lock,
including tree and type-compatibility edits. Reservation writes additionally
take a per-facility transaction advisory lock before conflict checks. Locks
remain held until commit; PostgreSQL commands require ReadCommitted isolation.
Permission-tree creation/moves also lock security source/projection tables before
reading or authorizing state; moves use audited token-checked reassociation
under dedicated Move authority. Updates, moves, deletion, and transitions
require the server-issued `updated_at` token. Actor and audit timestamps are
derived by authorized staging, never accepted from command input.
