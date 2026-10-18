# 77.20 — RTF templates

- Added persistent version-scoped RTF template store, backed by files and metadata in a configurable directory.
- Added upload/list/download/delete endpoints and schema-derived starter template generator.
- Added secure submission renderer, supporting `[[W:...]]`, `[[F:...]]`, `[[I:...]]`, `[[E]]`, CLASSNAME and FILLEDID.
- Preserves original RTF bytes and encodes dynamic Unicode values using RTF `\\uN?` escapes.
- FormVersion Read/Update and Submission Read required for corresponding operations.
- No database migration needed; mount a persistent folder for templates in production.
- .NET compile/runtime integration not performed in this environment.
