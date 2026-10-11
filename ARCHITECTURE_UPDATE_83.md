# ZArchitecture 83 — FormDataContext and local display.url

## Scope

- `IFormDataContextBuilder` assembles `{form,request,submission,workflow,history}` with stable FormNode technical keys. Values are provided by the caller and **must already be RBAC/field-level filtered**. A value not supplied is omitted (not silently substituted from inaccessible state).
- `FormContextMetadata` carries Request ID, current Submission ID, **WorkflowVersion ID** and current WorkflowState ID/name.
- `FormContextHistoryEntry` represents a completed transition. `StateId` is the **FROM** state, `ArchivedSubmissionId` is the archived Submission from that state, `ActorUserId` identifies the actor, `PublicNote` is public-only, `ChangedAt` is UTC. No extra persisted snapshots of state name or email. Entries are sorted oldest first; the underlying history remains append-only.
- Local `IFormExpressionEvaluator` resolves `$`, dotted object names and nonnegative `[index]` from JSON, without remote calls or code execution.
- Local `IFormUrlTemplateRenderer` replaces `{$.form.section.field}` placeholders, URI-escapes the substituted scalar and refuses missing data, non-HTTP(S) schemes or credentials in links.
- `display.url` uses `{ "template": "https://data.company.com/users/{$.form.sekce_a.user_id}", "label": "Profile", "target": "_blank" }` — **no JSONata request**. The designer passes the full FormNode tree to the renderer.
- Existing `IFormCalculationService` remains for the dynamic `input.select` and existing JSONata integration.

## Integration notes and limitations

- `FormDataContextBuilder.Build` is a pure transformation. Callers must supply the **visible** values from their RBAC-enforced services, and the **public** history. Never populate it from unrestricted entities for a client-side view.
- History DTOs can be mapped from existing `RequestTransitionHistoryResponse.from_state_id`, `.archived_submission_id`, `.actor_user_id`, `.note`, `.changed_at`, enriching `state.name` and `who.email` by ID when permissible. This version introduces the reusable contract; it does **not** add a new runtime query endpoint to materialize the context.
- The current form designer passes no live values by default, so expressions with field placeholders stay unresolved until visible values or a prebuilt `FormContext` are provided by the rendering host. Constant URLs work immediately.
- Repeated FormNode sections are not yet materialized as arrays by this builder. The evaluator supports indexing into arrays supplied by the caller.
- The designer and current server-rendered HTML are not fully reactive to unsaved browser-side edits. Live recomputation needs browser-side state wiring, not a JSONata call.
- No database schema changes or PostgreSQL migration.
- Dotnet SDK is not present in the artifact environment; compilation and integration tests were not run.
