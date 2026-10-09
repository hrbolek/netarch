# 77.15 — Accept pro JSONata

- `FormBackendHeaderForwarder.Apply` vždy nastavuje `Accept: application/json`.
- Příchozí `Accept` se záměrně nepřeposílá, ani je-li uvedeno ve `ForwardHeaders`.
- Společné pro GraphqlLikeFormDataBackend a JsonataPlayground.
- Ostatní pravidla ForwardHeaders beze změny.
