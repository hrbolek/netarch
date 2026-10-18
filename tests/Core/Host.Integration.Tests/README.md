# Core Host integration tests

Host startup, identity propagated from the trusted proxy header and common OpenAPI infrastructure. App-specific endpoint tests live in `tests/Apps/`.

The [security boundary framework](SecurityBoundary/README.md) adds strict
production-route HTTP contracts, compiled/project dependency checks, and a
generated endpoint coverage inventory. Real service RBAC and relational
transaction tests live separately in `tests/Core.AuthorizationLayer.Tests`.
Passing mocked resolver tests alone is not proof of correct authorization.
