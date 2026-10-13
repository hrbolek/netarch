# Entity model scaffold

Read `../ENTITY_CONTRACT.md` first. Defines a sample secured model, response projection and update DTO. `EntityNameCreateContainedRequest` supplies a requested container `group_id` — NOT a freely assignable `rbacobject_id`. Implement service-owned create using `CreationScopeGuard` as specified by `../ENTITY_CONTRACT.md` and choose the actual containment business rule; do not trust arbitrary security ids.

This is a partial scaffold, not a standalone complete entity implementation. Generate its sibling layers and verify compilation.
