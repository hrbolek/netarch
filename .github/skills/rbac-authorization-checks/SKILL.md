---
name: rbac-authorization-checks
description: Verify trusted actor identity, operation and scope filtering, transactional writes, and denial behavior in ZArchitecture.
---

## When to use

Use for changes to authorization, entity visibility, relation/vector reads,
self-service commands, create scope, updates, deletes, or sensitive data flow.

## Prerequisites

Read `ZA-AUTH-001` and `ZA-LAYER-001`, then inspect the actor provider,
`IRbacAuthorizationService`, `IAuthorizationLayer`,
`SecuredEntityService<T>`, and `TreeSecuredService<T>` applicable to the path.
Use `src/Apps/Events/README.md` for its documented narrow command exceptions.

## Procedure

1. Trace the actor from trusted claims/provider to the service and authorization
   layer. Confirm no request field can select the acting user.
2. For anonymous requests, verify failure occurs before actor-dependent work.
3. Trace read operation and scope into the actual secured query. Check related
   entities independently where their fields or existence are sensitive.
4. For create/update, verify the trusted target scope. Updates must check the
   original and any changed target scope; preserve the required concurrency
   token.
5. Verify writes stage audited changes and save transactionally. Audit identity
   and timestamps must not be accepted from request data.
6. Verify denial neither discloses protected existence nor mutates state.
7. For each unfiltered persistence path, identify its narrow invariant/command
   and its independent actor/scope binding.

## Architectural constraints

- Endpoint authentication/metadata does not substitute for service RBAC.
- Ordinary reads must use operation-filtered sources; writes use authorized
  staging and transaction helpers.
- Never broaden a raw query or privileged path for convenience.

## Validation

- Add positive and negative tests for each changed operation, including
  denial-state verification and concurrency/transaction cases where relevant.
- Run focused security and integration tests; use
  `./tools/audit-architecture.sh` for architecture changes.

## Expected outputs

Return the actor→operation→scope→query/write trace, evidence for allow/deny
paths, reviewed privileged exceptions, tests run, and any disclosure,
transaction, or concurrency concern.
