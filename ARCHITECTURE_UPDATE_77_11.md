# 77.11 – JSONata diagnostic playground

- `GET /jsonata` Development-only tester using the same configured backend.
- `POST /jsonata/execute` accepts `{ "query": "...", "variables": {} }`, forwards exactly the allowlisted headers using `FormBackendHeaderForwarder.Apply`, and returns outgoing headers, payload, full upstream response and errors.
- **Security:** diagnostic response intentionally contains credential values and possibly personal data. It is mapped only for `ASPNETCORE_ENVIRONMENT=Development`. Restrict network access to the development host.
- Header policy is exclusively in application config `DigitalProcesses:DataBackend:ForwardHeaders`.
- If testing with browser, `cookie` is sent to `/jsonata/execute` when same-origin; an Authorization bearer injected only by external clients must be provided to the development host or gateway.
