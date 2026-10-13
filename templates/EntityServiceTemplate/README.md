# Entity service scaffold

Use `../ENTITY_CONTRACT.md` for secure, transactional creation with `CreationScopeGuard`, staging and `SaveChangesAsync`; The hierarchy and actor-specific invariants are documented in the entity contract. Generated `EntityNameService` intentionally only provides secured base CRUD and **must be extended** for real domain creation, invariants, and side effects. Read `../ENTITY_CONTRACT.md`.
