## Architecture

The application uses a **domain-oriented layered architecture**.

Each application domain or entity is implemented as a separate module, typically containing:

- `Models`
- `Services`
- `Resolvers`
- `UI`

Common reusable mechanisms are implemented in `Base`.

### Dependency flow

The fundamental dependency flow is:

`Resolver -> Service -> Data Source`

A layer must not bypass the layer responsible for the requested capability.

### Data Sources

Data access is always hidden behind an interface such as `IUserStore`.

The Data Source layer is the only layer allowed to directly interact with databases, persistent storage, external data providers, or equivalent low-level data mechanisms.

Services depend on data-source abstractions, never on concrete persistence implementations.

Concrete implementations are selected through Dependency Injection.

### Services

Services contain application and domain behaviour.

A Service may use:

- its dedicated Data Source abstraction,
- other Services.

Services must be independent of transport and presentation technologies.

Therefore, Services must not depend on:

- HTTP concepts,
- `HttpContext`,
- `IResult`,
- routes or endpoints,
- Razor components,
- Resolvers.

### Resolvers

Resolvers are thin transport adapters.

Their purpose is to expose Services through HTTP/HTTPS endpoints.

A Resolver should normally:

1. receive and validate transport input,
2. invoke a Service,
3. translate the result into an HTTP response.

Resolvers must not implement business logic or access Data Sources directly.

### Models

Models and DTO contracts are defined independently of Resolvers.

Persistence models and externally exposed DTOs are separate concepts.

Conversion between database models, create/update requests, and response DTOs is explicit.

Do not expose persistence models directly merely because doing so would require less code.

### Base abstractions

Generic and reusable behaviour belongs in `Base`.

Domain-specific projects should specialize or compose these abstractions rather than duplicate generic CRUD, resolver, or storage behaviour.

### Dependency Injection and Host

Dependency Injection is the standard composition mechanism.

`Program.cs` / Host acts as the **composition root** where abstractions are mapped to concrete implementations and endpoint modules are registered.

The Host may compose the application but must not contain domain logic.

### General rule

Prefer:

`HTTP -> Resolver -> Service -> IDataSource -> Data Source`

Never introduce shortcuts such as:

`Resolver -> IDataSource`

`Resolver -> Database`

`Service -> Resolver`

`Service -> HTTP`

`Data Source -> Service`

Existing code that violates these rules must not be treated as an architectural precedent.