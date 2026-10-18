# ARCHITECTURE UPDATE 85

Base: ZArchitecture_84. No database migration.

## Changes
- `GET /digital-processes/form-component-catalog`: RBAC-filtered inventory of registered known renderer types, with descriptions, capabilities and complete `configuration_example` objects for PydanticAI.
- Form designer node properties: read-only `$.form...` structural path, computed through parent IDs, and copy button. For display/action nodes the path is structural, not a materialized data value.
- Local URL template renderer: limits template length (4096), expression count (32), expression length (512), rendered length (8192); returns error result rather than throwing for malformed expression/JSON value; restricts URL to HTTP(S) without credentials.
- `ComputedUrlRenderer` now degrades to disabled link instead of propagating any parsing/runtime error.

## Validation
ZIP creation and source inspections only. .NET SDK was unavailable in this execution environment; compile/integration tests not executed. Existing data context / preview value wiring limitations remain from version 84.

## Known limitations
Component catalog examples are in code and should be synchronized as renderer configuration changes. URL interpolation percent-encodes path replacement values. Form path display is server-rendered at dialog opening; it reflects a new key after form save/reload. Validate the catalog's examples against individual renderer capabilities before treating them as a strict schema.
