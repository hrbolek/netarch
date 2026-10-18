# Facilities reservations and authorization

Status: Accepted

Date: 2026-10-18

Scope: reservation policy and clarified Facility permission-tree ownership,
explicitly approved by the requester.
Event lifecycle integration below remains Proposed.

Rules: ZA-LAYER-001, ZA-GENERIC-001, ZA-AUTH-001, ZA-API-001,
ZA-MIGRATE-001.

Context: the new Facilities domain needs an Event association without changing
existing Events contracts or confusing physical containment with RBAC groups.
The existing service authorization boundary and module composition described
in [the architecture rules](../rules.yml) remain authoritative.

## Accepted policy

Facilities own physical locations, flat facility types, and reservations.
Physical containment is not itself an RBAC group hierarchy. Each Facility must
own a distinct RBAC object in a separate Group-based permission tree. Existing
shared groups must not be reparented merely because a Facility moves.

The requester clarified that creating a child requires Create on its parent.
A Facility subtree move must build a **copy** of the existing permission
subtree beneath the destination's permission group, preserving the copied
roles and memberships. The destination's ancestor rules then apply, and
EffectiveAccess must be recalculated. This supersedes the initial proposal to
preserve shared explicit scope IDs during a physical-only move.

The implementation must retain the distinction between copying permission
source data and refreshing its derived EffectiveAccess projection. Refreshing
unchanged scopes alone does not implement the requested move semantics.
The requester additionally specified a dedicated `move` permission on the
existing RBAC object of the moved root. The destination requires Create.
This does not implicitly require Update on every descendant. No existing
role is silently granted Move.

Initial implementation constraints:

- Creating a root Facility uses an explicitly selected existing permission
  parent; creating a child uses its Facility parent's owned permission Group.
  Each new Facility receives its own new Group/RBAC object.
- Move requires a destination Facility. Moving to a physical root without an
  explicit destination permission parent is outside the initial command.
- Copy the source Group subtree with fresh IDs and recomputed Group paths;
  remap affected Facility scopes to the new IDs. Never reparent, delete or
  expire the original Groups, roles or memberships.
- Preserve role/membership validity and descendant inheritance flags. The
  cloned root inherits destination ancestor rules. Copy/remap deny rules;
  unsupported access bindings or deputy references fail closed rather than
  copying authority to external targets.
- The narrowly scoped copy command may use existing audited persistence APIs
  after its explicit Move/Create checks. Ordinary CRUD remains secured.
  Source snapshot consistency, clone writes, physical path updates and
  EffectiveAccess synchronization must share one transaction.
- Reject mismatches between copied permission scopes and the physical Facility
  subtree rather than reassociating unrelated Facilities or retaining an
  unmapped moved descendant.

Old-tree garbage collection and binding/deputy extensions require separate
ownership and authorization decisions. They must not be implemented as a
side effect of this feature.

Use existing RBAC operations, not new operation names:

| Operation | Required access |
|---|---|
| Request reservation | Read Facility, Update Event, Create on the explicit reservation scope |
| Confirm, reject, cancel | Update Facility, Update Event, Update Reservation |
| Read related reservations | Read Reservation, Read Facility, Read Event |

Service authorization is mandatory independently of HTTP actor checks.
Reservation transitions must not expose generic update paths that could
circumvent these checks or alter a confirmed reservation's interval.

The association belongs to Facilities: a separate reservation references one
Facility and one Event. Multiple reservations support multiple facilities per
event and many events per facility over time. Events must not acquire a
dependency on Facilities. Occupancy is derived from confirmed reservations,
using half-open intervals `[start, end)`; pending requests do not occupy space.
Confirmation must serialize conflicting writes at the database/transaction
boundary, not merely perform an unprotected read before writing.

## Event lifecycle integration: proposed, not implemented implicitly

Existing Events contracts and behavior are unchanged. No automatic reservation
cancellation or rescheduling hook is added by this decision.

Alternatives requiring a separate approval:

1. An application-level coordinator invokes both modules in one transaction
   when an event is rescheduled or cancelled. This requires an agreed Events
   command extension point and cross-module authorization/rollback contract.
2. Events publish transactional lifecycle notifications; Facilities reconciles
   reservations. This adds an outbox, delivery/retry semantics, and eventual
   consistency.
3. Keep explicit reservation cancellation/rebooking, without automatic
   propagation. This is the initial behavior and requires operators to manage
   both changes explicitly.

Do not silently introduce a reverse module dependency, infer event cancellation
from unrelated fields, or rewrite a confirmed booking when an Event changes.
Existing Event deletion behavior and reservation foreign-key behavior must be
documented and tested as part of the domain implementation.

The requester explicitly approved restrictive deletion on 2026-10-18:
an Event with reservation history cannot be physically deleted. Reservations
remain intact, including cancelled/rejected history. This is the deliberate
effect of the new foreign key, not an implicit cancellation/rescheduling hook
or a modification of the Events service contract.
