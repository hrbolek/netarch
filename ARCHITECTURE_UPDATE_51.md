# Architecture Update 51 — Incremental EffectiveAccess rebuilds

This update reduces authorization-projection overhead for common create and Membership mutations.

## New projection API

`ISecurityProjectionService` now exposes:

- `RebuildAllAsync()` — complete projection rebuild; retained for startup/repair and broad-impact changes.
- `RebuildRbacObjectAsync(Guid)` — rebuild one exact RBAC object.
- `RebuildRbacObjectsAsync(IEnumerable<Guid>)` — rebuild several exact RBAC objects.

Targeted rebuilds delete and recreate only `EffectiveAccess` rows for the requested RBAC object ids. The authoritative Role / Group / Membership / AccessBinding graph is still evaluated, so resulting access semantics are unchanged.

## Digital Processes

The following creates now rebuild only the newly created security scope:

- FormDefinition
- WorkflowDefinition
- ProcessDefinition
- FormNodeType
- SubmissionNode when it creates an own security scope

This removes the previous global `EffectiveAccess` purge/rebuild from ordinary definition creation.

## Membership

Membership mutation affects the security object of the related user. Therefore:

- create rebuilds the affected User RBAC object;
- update rebuilds old and new User RBAC objects;
- delete rebuilds the affected User RBAC object.

## Deliberately still global

`RebuildAllAsync()` remains for changes whose impact can cross a hierarchy or several exact targets and therefore needs a separate impact model before it can safely be narrowed:

- Group hierarchy changes;
- Role create/update/delete;
- User service operations that currently change membership/security topology;
- complex Digital Processes Request start/transition flows;
- application startup / dump restore repair.

A future optimization can introduce `RebuildGroupSubtreeAsync` and role-impact rebuilds without changing runtime authorization semantics.
