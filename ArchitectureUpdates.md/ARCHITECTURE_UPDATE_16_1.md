# Architecture Update 16.1

Build cleanup for Update 16.

- Fixed the missing closing brace in `SecurityProjectionService`.
- Removed fake `async` declarations from stage-only EF store create/update methods.
- These methods now return `Task.FromResult(...)` because they only modify the EF Core change tracker and perform no asynchronous I/O.
- No transaction or persistence semantics were changed. `SaveChangesAsync` remains the explicit persistence boundary.
