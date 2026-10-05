# Architecture Update 29

## Application navigation

Update 29 introduces application-aware navigation shared by all Razor pages.

The application registry lives in:

- `src/Core/Base/UI/Routing/ApplicationNavigationRegistry.cs`

It contains application metadata and navigation items. `Page.razor` resolves the active application from the first URI segment and renders the corresponding menu.

Registered applications:

- `/architecture`
- `/digital-processes`

Every application menu contains a common `Home` link to `/`.

## Home page

`/` now renders `ApplicationHomePage.razor` and provides entry cards for all registered applications.

Dedicated application landing pages are available at:

- `/architecture`
- `/digital-processes`

The old diagnostic JSON previously served from `/` is available from `/info`.

## Architecture navigation

The existing Architecture navigation has been moved from hard-coded Razor markup into the navigation registry. Context-sensitive User/Group Memberships and Roles menu items are preserved and are enabled only when the current URI contains the required entity and identifier.

## Digital Processes navigation

Digital Processes defines its navigation in:

`src/Apps/DigitalProcesses/DigitalProcesses.UI/Routing/DigitalProcessNavigationRegistration.cs`

Sections include:

- Requests
- Processes
- Forms
- Workflows
- Administration

Only currently available browser GET/list pages are linked. Create links are intentionally not added yet because most DigitalProcesses create operations are POST-only API endpoints and do not yet have dedicated Razor create pages.

## Swagger/OpenAPI descriptions

DigitalProcesses API endpoints now include `.WithDescription("""...""")` metadata with business-oriented descriptions.

Descriptions were added for:

- form definition/version/node operations
- workflow definition/version/state/transition/access-rule operations
- process definition/version operations
- request start, transition and detail
- submission tree and submission-node operations
- generic DigitalProcesses view/list/link endpoints
- Home and application landing endpoints

The workflow transition description explicitly documents submission archival, new materialized paths, new RBAC scopes, AccessBinding materialization and transition history.

## Notes

No database schema changes are introduced by Update 29.

The environment used to prepare this update does not contain the .NET SDK, therefore the update was statically inspected but not compiled here.
