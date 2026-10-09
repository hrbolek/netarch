# ZArchitecture 77.6 – GraphQL provider / shape diagnostics

- Default Host `appsettings.json` previously selected `Dummy`, returning an array of users rather than GraphQL `{data:{...}}`. This exactly leads to `Backend returned an invalid GraphQL response.` unless `ASPNETCORE_ENVIRONMENT=Development` or env overrides are set.
- Set base configuration to `Provider=GraphQL`, `Endpoint=http://serverjsonata:3000/graphql`. **Adjust the path to the real working GraphQL route** (`/graphql`, `/api`, etc.). Docker env `DigitalProcesses__DataBackend__*` still takes priority.
- Preview endpoint reports whether received response is an array, scalar, null or missing `data`, and top-level field names. GraphQL transport logs response shape (not values, cookies, or tokens).
- Reload/restart the dotnet app after changing configuration; check runtime env vars because they override appsettings.
- Existing design/test HTTP action, preview state and HTML fragments remain unchanged.
- ZIP validated, .NET SDK unavailable in authoring environment.
