# ZArchitecture 94 — Razor-first HTML presentation

## Architecture rule

HTML views and editors are authored in Razor (`.razor`) components, using shared `DigitalProcessLargeCard` / `LargeCard`. Minimal API endpoint handlers do not assemble HTML with `StringBuilder`, string interpolation, or `Results.Content` in normal presentation paths. They are responsible for authorization, loading data and supplying Razor view models. JavaScript is reserved for client interaction and calling authorized APIs, not creating the page layout.

## Changes

- `/digital-processes/form-versions/{id}/templates`: replaced the hand-assembled HTML ContextShell and page with `FormTemplatesPage.razor`, rendered by `RazorComponentResult` and wrapped in `DigitalProcessLargeCard<FormVersionResponse>`. Upload/generate/list actions preserve their existing API routes.
- `/digital-processes/process-versions/{id}/permissions`: replaced hand-assembled HTML with `ProcessPermissionsPage.razor`, wrapped in `DigitalProcessLargeCard<ProcessDefinitionVersionResponse>`. State and node lists are passed as typed DTOs. Draft/Update authorization logic is kept on the server. Matrix is now ordered by tree traversal / sibling position rather than path text.
- Removed `Shell`, `ContextShell`, `StringBuilder` and inline HTML factories from `DigitalProcessesManagementPages.cs`.

## Known limits

- The matrix's inherited/unspecified state still cannot be saved, because the existing Applicant access API provides GET and PUT but no DELETE for removal of an explicit rule. This release does not silently alter its backend contract.
- Scripts within Razor are retained for browser-side interactions. Subsequent improvements may extract them into JS modules, without reverting to server-side HTML string generation.
- The container does not have .NET SDK, therefore compilation and runtime tests could not be performed here. Verify `dotnet build SignFakeArchitecture.sln` and run the listed endpoints in the target environment.
