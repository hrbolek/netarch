# ZArchitecture 77.4

Fix HTMX import POST with Content-Length: 0. Explicitly include `#dp-preview-form` and hidden preview identity inputs in the import card. Keep hidden IDs in returned success/error fragments for retry after HTMX swap. Improve diagnostics for missing identity.

Test: open Test mode, click Import, Network > Payload should show `previewInstanceId`, `formVersionId`. Server should advance to preflight.

Note: .NET build unavailable in authoring environment.
