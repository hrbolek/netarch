# ZArchitecture 77.9

- Add-node `configuration_json` fields are empty; example GraphQL JSON appears only in placeholders.
- Node `key` uniqueness is scoped to `form_version_id` + `parent_id` for create/update. Subtree cloning checks only imported root against target siblings; moving checks destination siblings.
- GraphQL HTTP body is logged at Trace, full GraphQL response inspectable in **Test** mode via expandable diagnostic details, including `data` / `errors` and matching counts.
- `appsettings.json` enables Trace for the GraphQL transport and the preview import logger. **Warning:** full GraphQL payloads may contain personal information. Restrict logs to development and protect access to Test mode; restore Information for production.
- No .NET build verification was possible in this environment.
