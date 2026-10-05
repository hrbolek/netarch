# Architecture Update 28

## Build fixes

- Fixed `GroupResolvers.Update` after adding `GroupUpdateRequest.inherit_roles`.
  The resolver now preserves `existing.inherit_roles` and passes it as the fifth
  constructor argument before `updated_at` and `updated_by`.
- Suppressed `AD0001` only in `DigitalProcesses.Resolvers`. The message is emitted
  because `Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer`
  throws an internal `NullReferenceException` while analyzing the generic Minimal
  API endpoint composition. It is not a source compiler error.

No runtime Security query was changed.
