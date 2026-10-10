-- Apply once to postgres_development, after backing up the database.
-- Idempotent CREATE statements for the new template history tables.
CREATE TABLE IF NOT EXISTS dp_document_template_revisions (
    id uuid PRIMARY KEY,
    template_id uuid NOT NULL REFERENCES dp_document_templates(id) ON DELETE CASCADE,
    revision integer NOT NULL,
    content bytea NOT NULL,
    content_type text NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT uq_dp_template_revision UNIQUE (template_id, revision)
);
CREATE TABLE IF NOT EXISTS dp_template_field_bindings (
    id uuid PRIMARY KEY,
    revision_id uuid NOT NULL REFERENCES dp_document_template_revisions(id) ON DELETE CASCADE,
    placeholder text NOT NULL,
    form_node_id uuid NOT NULL REFERENCES dp_form_nodes(id) ON DELETE RESTRICT,
    CONSTRAINT uq_dp_template_field_binding UNIQUE (revision_id, placeholder)
);
-- Existing RTF templates predate versioning. Preserve their original content as revision 1.
INSERT INTO dp_document_template_revisions
    (id, template_id, revision, content, content_type, created_at)
SELECT gen_random_uuid(), t.id, 1, t.content, t.content_type, t.created_at
FROM dp_document_templates t
WHERE NOT EXISTS (
    SELECT 1 FROM dp_document_template_revisions r WHERE r.template_id = t.id
);
