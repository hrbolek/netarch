# ZArchitecture 77.21 – Applicant policy / RBAC deny

- New secured policy rows `dp_form_node_applicant_access` and `rbac_deny_rules`.
- REST endpoints GET/PUT for Applicant read permission on FormNode in process/state context.
- SecurityProjectionService applies persistent, subtree-propagating role denies
  to EffectiveAccess, including mappings from functional roles.
- RequestService gives Applicant positive bindings to permitted RBAC scopes and
  materializes recursive Deny on hidden branches on initial Request and each transition.
- SubmissionNodeService carries the same logic into runtime/repeated nodes.
- On archive, the old Submission and its RBAC tree/policies stay unchanged.
- State transitions copy all stored values through an internal trusted reader,
  without inadvertently discarding fields hidden from the acting user.
- SQLite startup idempotently adds new tables/indexes to existing developer DBs.
- See README for endpoint shapes and caveats; local dotnet compilation was unavailable.
