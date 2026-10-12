-- Execute against an existing PostgreSQL database BEFORE deploying version 95.
-- New installations are created through EF Core model configuration.
-- Existing process definitions are intentionally left unassigned until their historical
-- versions have been audited. Do not arbitrarily choose among multiple form/workflow definitions.
BEGIN;
ALTER TABLE dp_process_definitions ADD COLUMN IF NOT EXISTS form_definition_id uuid NULL;
ALTER TABLE dp_process_definitions ADD COLUMN IF NOT EXISTS workflow_definition_id uuid NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ix_dp_process_definitions_form_definition_id
    ON dp_process_definitions (form_definition_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_dp_process_definitions_workflow_definition_id
    ON dp_process_definitions (workflow_definition_id);
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_dp_process_definitions_form_definition_id') THEN
    ALTER TABLE dp_process_definitions ADD CONSTRAINT fk_dp_process_definitions_form_definition_id
      FOREIGN KEY (form_definition_id) REFERENCES dp_form_definitions(id) ON DELETE RESTRICT;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_dp_process_definitions_workflow_definition_id') THEN
    ALTER TABLE dp_process_definitions ADD CONSTRAINT fk_dp_process_definitions_workflow_definition_id
      FOREIGN KEY (workflow_definition_id) REFERENCES dp_workflow_definitions(id) ON DELETE RESTRICT;
  END IF;
END $$;
COMMIT;
-- Audit existing process definitions, including those with multiple different references:
SELECT pd.id, pd.name,
       count(DISTINCT fv.form_definition_id) AS distinct_forms,
       count(DISTINCT wv.workflow_definition_id) AS distinct_workflows
FROM dp_process_definitions pd
LEFT JOIN dp_process_definition_versions pv ON pv.process_definition_id = pd.id
LEFT JOIN dp_form_versions fv ON fv.id = pv.form_version_id
LEFT JOIN dp_workflow_versions wv ON wv.id = pv.workflow_version_id
GROUP BY pd.id, pd.name ORDER BY pd.name;
