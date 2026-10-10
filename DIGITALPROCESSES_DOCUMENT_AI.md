# DigitalProcesses: Document actions and AI placeholders

## Document templates

The RTF template storage was moved to `dp_document_templates` in PostgreSQL. The existing RTF endpoints and RBAC checks are preserved; the schema bootstrap creates the table for existing dev databases. No recurring filesystem dump is involved. `created_at` is UTC.

## action.document

The new node type is seeded at host startup. A designer creates a node with `renderer_key=action.document`, and a saved `configuration_json` such as:

```json
{"templateId":"PUT-A-TEMPLATE-ID-HERE"}
```

Set `DigitalProcesses:DocumentProcessing:Endpoint` to a **trusted internal HTTP(S) endpoint** accepting multipart fields `file` (RTF), `formVersionId`, and `previewInstanceId`. The configured service must return GraphQL-like JSON `{"data":{...}}`. The current preview action **reuses existing import response mapping**, including section-relative key matching. The endpoint never comes from editable FormNode JSON.

The action currently operates in **form designer Test/Preview mode**, not over persisted runtime submissions. The submission tree shows an AI placeholder, not a working document action. RTF output is kept in memory for the transport and isn't yet stored in a GeneratedDocument history table.

The importer uses the existing PreviewFormInstanceStore and does not write runtime submissions. External endpoints must be trusted and protected with network policy; no arbitrary user-directed URL forwarding.

## AI placeholders

Both UI placeholders are intentionally disabled. The design placeholder identifies the form version; runtime placeholder identifies a submission. Future MCP agents must use existing authenticated Services/authorization, show proposed diffs and require human approval before modifying data. Historical submissions should be selected under caller permissions.

## Tests and migrations

These changes were statically checked but could not be compiled in this environment. Run the existing 39 DigitalProcesses integration tests and verify the preview action with a mock multipart JSON endpoint. The development schema bootstrap uses guarded PostgreSQL `CREATE TABLE IF NOT EXISTS`; replace it with EF migrations when a managed migration strategy is adopted.
