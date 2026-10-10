-- ZArchitecture 80: repair missing base document template table (PostgreSQL).
-- Run once on postgres_development. Safe with existing rows, no DROP/TRUNCATE.
-- Start with psql -v ON_ERROR_STOP=1 -1, or a pgAdmin transaction.
CREATE TABLE IF NOT EXISTS dp_document_templates (
    id uuid PRIMARY KEY,
    form_version_id uuid NOT NULL REFERENCES dp_form_versions(id) ON DELETE RESTRICT,
    name text NOT NULL,
    content_type text NOT NULL,
    content bytea NOT NULL,
    created_at timestamp with time zone NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_dp_document_templates_form_version_id"
    ON dp_document_templates (form_version_id);

CREATE TABLE IF NOT EXISTS dp_document_template_revisions (
    id uuid PRIMARY KEY,
    template_id uuid NOT NULL REFERENCES dp_document_templates(id) ON DELETE CASCADE,
    revision integer NOT NULL,
    content bytea NOT NULL,
    content_type text NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT uq_dp_template_revision UNIQUE (template_id, revision)
);
CREATE INDEX IF NOT EXISTS "IX_dp_document_template_revisions_template_id_revision"
    ON dp_document_template_revisions (template_id, revision);

CREATE TABLE IF NOT EXISTS dp_template_field_bindings (
    id uuid PRIMARY KEY,
    revision_id uuid NOT NULL REFERENCES dp_document_template_revisions(id) ON DELETE CASCADE,
    placeholder text NOT NULL,
    form_node_id uuid NOT NULL REFERENCES dp_form_nodes(id) ON DELETE RESTRICT,
    CONSTRAINT uq_dp_template_field_binding UNIQUE (revision_id, placeholder)
);
CREATE INDEX IF NOT EXISTS "IX_dp_template_field_bindings_revision_id_placeholder"
    ON dp_template_field_bindings (revision_id, placeholder);
CREATE INDEX IF NOT EXISTS "IX_dp_template_field_bindings_form_node_id"
    ON dp_template_field_bindings (form_node_id);

-- Backfill revision 1 from pre-existing templates, if there are any.
INSERT INTO dp_document_template_revisions (id, template_id, revision, content, content_type, created_at)
SELECT gen_random_uuid(), t.id, 1, t.content, t.content_type, t.created_at
FROM dp_document_templates t
WHERE NOT EXISTS (
    SELECT 1 FROM dp_document_template_revisions r WHERE r.template_id = t.id
);
