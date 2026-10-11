# Form Design Agent – ZArchitecture 88

Run from `tools/formdesign-ai-agent` parent using `uvicorn form_designer_agent.main:app --host 0.0.0.0 --port 9088`.
Set `AI_BASE_URL`, `AI_API_KEY`, optionally `AI_MODEL`.
POST `/agents/form-design` accepts `contract`, `request_id`, `instruction`, and `context` containing `form_version_id`, a **tree** `form_schema.children`, and `component_catalog`.
The agent returns `{contract,request_id,result:{children:{...}},warnings:[]}`. Its output is only a proposal; ZArchitecture approves/persists separately.
