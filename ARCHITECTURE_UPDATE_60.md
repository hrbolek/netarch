# Update 60 – Definition detail actions and request lookup

- WorkflowDefinition detail now lists RBAC-readable workflow versions and opens the workflow designer; new draft and publication actions are available.
- WorkflowDefinition creation already atomically initializes version 1. Existing pre-update workflow definitions may still lack versions; create a draft from the detail.
- FormDefinition detail adds new draft and publish actions; publishing calls the existing business validation.
- ProcessDefinition detail lists versions and offers creation of a process version by selecting linked entities.
- Request creation uses EntityLookupField to select a published process version rather than entering a GUID.
- The lookups are RBAC-limited, capped at 25 entries; filtering of process names starts at two characters.

Limitations: creating an additional version creates a new draft but does not clone the content of an older published version. User-facing publishing controls call server-side methods but a final .NET build could not be run in this environment.
