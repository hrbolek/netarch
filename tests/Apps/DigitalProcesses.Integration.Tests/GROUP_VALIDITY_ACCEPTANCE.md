# Group validity and target selection (DP24–DP25)

- `GroupModel.valid` is persisted as `groups.valid` (DB default true). It represents organisational validity, not user membership.
- Published `ProcessDefinitionVersionModel.target_group_type_id` is required and immutable after publishing.
- `StartRequestRequest.target_group_id` selects the actual DB group. Start verifies valid, exact group_type_id and caller Create via secured query.
- Failed checks take place before any Request/Submission creation.
- Previous Requests and Submission histories are unaffected when the organisational group is retired; historical RBAC groups remain stored.
- Current implementation supports one `TargetGroupTypeId` per published version. Multiple types/explicit group IDs require a separate versioned rule table.
- Tests DP24/25 cover policy predicate and Group default flag. Add full HTTP lifecycle fixture + cross-context authorisation tests before claiming end-to-end acceptance.
- `EnsureCreated` SQLite will create new columns; existing databases require a schema migration (see SQL note below).

Example migration (adapt SQL to selected DB provider): `ALTER TABLE groups ADD COLUMN valid BOOLEAN NOT NULL DEFAULT TRUE;` and add nullable `target_group_type_id` to process-definition-versions table.
