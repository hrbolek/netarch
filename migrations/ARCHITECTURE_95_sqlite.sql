-- For an existing SQLite development database only; back it up before execution.
-- SQLite ALTER TABLE ADD COLUMN does not retroactively create FK constraints.
-- The full FK constraints are present for newly-created databases via EF Core.
ALTER TABLE dp_process_definitions ADD COLUMN form_definition_id TEXT NULL;
ALTER TABLE dp_process_definitions ADD COLUMN workflow_definition_id TEXT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ix_dp_process_definitions_form_definition_id ON dp_process_definitions(form_definition_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_dp_process_definitions_workflow_definition_id ON dp_process_definitions(workflow_definition_id);
-- Existing rows are intentionally left NULL: audit their version history and
-- resolve conflicts explicitly, then assign both columns. Do not guess ownership.
