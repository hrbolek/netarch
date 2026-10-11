# ARCHITECTURE_UPDATE_88

- Form-design request now sends `context.form_schema` (hierarchical tree keyed by FormNode technical keys), not a flat `context.nodes` array. Existing node UUIDs, type and configuration are included. Invalid/incomplete hierarchy is rejected rather than silently omitted.
- `component_catalog` now includes `description` and `configuration_example` from the registered component catalog.
- `src/Host/appsettings.json` uses the supplied AiAgents configuration (formdesign enabled on `http://host.docker.internal:9088/agents/form-design`, 180 s).
- Updated FastAPI/PydanticAI agent under `tools/formdesign-ai-agent` uses typed `DesignRequest`, `FormSchema`, and `FormDesignerDeps`; its tool `get_current_form()` returns the hierarchical schema.
- Output contract remains `result: FormSchema`, preserving existing IDs. No DB migration.
- Build/integration with live .NET/FastAPI model unavailable in this environment.
