# ZArchitecture 98 — FK navigation through EntityLink

## Change

- The `DigitalProcessForeignKeyLinks.razor` component now renders its related entity hyperlinks using the shared `EntityLink<EntityLinkResponse>` Razor component.
- Links are no longer created with manually formatted `/digital-processes/{entity}/view/{id}` HTML href values.
- `EntityLink` delegates URL resolution to the existing `IEntityRouteRegistry`, with explicit `App=digital-processes`, `Entity=target response type`, `Operation=view` overrides matching currently exposed routes.
- FK extraction and labels remain in `DigitalProcessForeignKeyLinks` for now. This retains typed relationships and existing `MediumCard` integration without creating a second link-rendering system.

## Notes

- The linked entity currently uses the GUID as display text, as its full label is not available in the source DTO. A later enhancement can batch-enrich labels while keeping `EntityLink` as the renderer.
- Authorization remains enforced at the linked endpoint.
- No database change is needed.

## Verification

- Source-level review completed. Dotnet SDK not available in this environment, so compilation and runtime tests have not been run.
