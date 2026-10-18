# ZArchitecture 77.10

- Host-only allowlist `DigitalProcesses:DataBackend:ForwardHeaders`, never taken from a form node.
- `GraphqlLikeFormDataBackend` forwards only explicitly named request headers to the configured backend endpoint.
- **Security:** deployment reverse proxy must strip user-supplied `x-ms-client-principal*` and inject authenticated values; forwarding cookies/tokens only to a trusted backend and only with appropriate audience. Never log header values.
- Form input `name` is now `node-{guid:N}`, and preview state is indexed by node ID, not `key`.
- GraphQL form variable paths are constructed from the node hierarchy; duplicate `id` keys in different sections are valid.
- Existing previews need a fresh page load due to changed field names.
- Server logging for GraphQL debugging is retained.
- No dotnet SDK available in generation environment; compile test not performed.
