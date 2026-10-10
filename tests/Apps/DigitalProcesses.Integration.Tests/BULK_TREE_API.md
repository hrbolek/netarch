# Batch APIs for agents and clients

* `PUT /digital-processes/FormVersionResponse/tree/{id}` accepts `{ "nodes": [{"key":"section", "node_type_id":"...", "children": [...]}] }`. Optional `id` means update of an existing node; omitted nodes are **preserved**, never implicitly deleted. Only drafts can be edited. The original parent/type of existing nodes cannot change in this operation. New nodes inherit the parent's authorization scope.
* `PUT /digital-processes/SubmissionResponse/tree/{id}` accepts `{ "nodes": [{"id":"...", "value_json":"\"text\""}] }`. Omitted nodes are preserved. All nodes must belong to the same non-archived Submission, be writable by the caller, have a value-bearing type, and contain valid JSON.
* Validation and per-node authorization complete before the transaction starts. All writes use one security transaction and one `SaveChangesAsync`. If any validation or authorization fails, nothing is written.
* Identity always comes from the HTTP authentication context. Neither API accepts `rbacobject_id`, user IDs or audit metadata.
* `FormNode` creation with separate security scopes, structural moves and deletion remain dedicated operations; these are deliberately **not** implicitly supported by the batch command.

## Remaining acceptance checks

Verify the new endpoint contracts with the local SDK and run integration tests. Add HTTP E2E tests for nested form creation, deny rollback, archived submission and simultaneous stale writes before considering the AI editing API production-ready. No concurrency token is introduced by this patch.
