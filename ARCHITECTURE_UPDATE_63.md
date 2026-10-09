# Architecture update 63 — Standard URI, vector lists and foreign-key selectors

## Analysis of reference endpoints

The `GroupResponse` resolver delegates `/architecture/GroupResponse/list` to `BaseResolvers.List<GroupModel, GroupResponse, IGroupService>`. The base resolver parses `where`, `take`, `skip`, `order_by` with `VectorQueryParser.TryParse`, invokes the service's `VectorPageAsync` with the requested RBAC operation, then returns DTOs and paging headers. The `/view/{id}` flow uses `BaseResolvers.Read` to fetch a single secured entity. The DTO projection is defined on the model response. No application-specific `lookup` route is necessary.

The former DigitalProcesses lookup routes were an architectural divergence: `/ProcessDefinitionVersionResponse/lookup/{kind}`, `/RequestResponse/lookup/process-versions` and `/WorkflowStateAccessRuleResponse/lookup/{kind}` implemented a second family of JSON response shapes and duplicated filtering, ordering, caps and authorization queries. In addition, the form/workflow version lookup did not search by the definition's name.

## Changes

- Removed the three custom `/lookup` routes and their ad-hoc query implementations.
- Reused the shared `EntityLookupSelect` component already present in Base.UI (same component as used for group/user/role selection).
- Entity selectors use `/digital-processes/{EntityResponse}/list` with standard `where` and `take=25` parameters, and send `Accept: application/json`.
- Without search text, up to 25 records are requested. At 2+ characters, `_ilike` is applied to configured text fields. Server-side filtering, projection, paging and RBAC remain handled by BaseResolvers and the corresponding service.
- Fixed `where` constraints (e.g. published versions) are combined using `_and` with text search and an optional parent selection constraint, avoiding duplicate `where` query parameters.
- Process version creation selects a FormDefinition using `/digital-processes/FormDefinitionResponse/list`, then limits FormVersion choices to published versions belonging to that definition. WorkflowDefinition and WorkflowVersion follow the same pattern.
- Request creation selects ProcessDefinition then its published ProcessDefinitionVersion.
- WorkflowStateAccessRule creation selects WorkflowState, RoleType (from `/architecture/RoleTypeResponse/list`) and FormNode using ordinary `/list` endpoints.
- Removed the redundant DigitalProcesses-specific `EntityLookupField.razor`.

## Contract examples

`GET /digital-processes/FormDefinitionResponse/list?take=25`

`GET /digital-processes/FormDefinitionResponse/list?where={"name":{"_ilike":"%žádost%"}}&take=25`

`GET /digital-processes/FormVersionResponse/list?where={"_and":[{"is_published":{"_eq":true}},{"form_definition_id":{"_eq":"<guid>"}}]}&take=25`

The JSON in query strings must be URL-encoded. `where` is optional and is parsed by the existing `VectorQueryParser` and `HasuraFilterExpression`; the actual response body is an array of entity DTOs, not a separate `{id,label}` projection.

## Verification and limitations

Static inspection confirmed all new selector targets have an existing `/list` resolver; the removed `/lookup` routes no longer occur in DigitalProcesses code. The ZIP archive was checked for integrity. .NET SDK is not available in this execution environment, so compilation and browser tests were not performed. Further review should evaluate all unrelated special-purpose read routes (e.g. designers and details), distinguishing genuine resource-specific views from redundant vector-list endpoints.
