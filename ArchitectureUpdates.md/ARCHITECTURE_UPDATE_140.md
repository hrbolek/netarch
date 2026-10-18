# ARCHITECTURE_UPDATE_140

## Changes
- Added `templates/ENDPOINT_CONTRACT.md`: canonical endpoint matrix, `ApplyViewModel`/`ApplyJsonModel`, Swagger `WithName`/`WithDescription`/`WithTags`, content negotiation, RBAC and verification.
- Reworked `EntityResolversTemplate` endpoint mapper into separate file; canonical `/EntityNameResponse` routes, real UI mappings and JSON-first update; added `BaseResolvers.Link`, and UI project reference.
- Extended template instructions to require endpoint metadata and remain self-contained with binary-only Core.
- Labeled unimplemented create, actions, form, vector flows explicitly; no fake handlers or invented API.
- Adjusted legacy `ApplicationTemplate` route shapes toward canonical paths, while retaining its partial-template status.

## Verification / limitations
ZIP integrity and static checks verified; .NET SDK not installed here, so compile, Swagger output and live endpoint behavior NOT verified. No changes to Core or production modules.
