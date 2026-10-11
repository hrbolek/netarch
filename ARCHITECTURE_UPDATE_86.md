# ARCHITECTURE_UPDATE_86

## Scope
- Minimal configurable FastAPI/PydanticAI `formdesign_aiagent` under `tools/formdesign-ai-agent`.
- `AiAgents` section in Host configuration, initially disabled, with named endpoints for form design, filling and document templates.
- `POST /digital-processes/ai/form-design` proxy endpoint on Host; checks Read on FormVersion, sends only readable FormNodes and component types, handles timeout and errors. Does not save output.
- Pydantic models for nested FormSchema with key validation, catalog-limited renderers.

## Not implemented
- Designer UI action, preview / diff and save flow; model output remains an untrusted suggestion.
- On-behalf-of delegation for `fillform_aiagent`; principal headers are not OAuth access tokens.
- End-to-end .NET compilation and integration tests have not been run here.

## Security guidance
No identity or application secrets are forwarded to agents in this phase. Deployment must restrict agent endpoint reachability, use HTTPS or trusted container network and service-to-service authentication. The AI result is NOT automatically persisted.

## Database
No migration.
