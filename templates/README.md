# ZArchitecture templates — standalone agent reference

`/templates/` is the **self-contained normative entry point** for creating domains, entities, forms and specialized views. It is designed for consumers that possess **Core only as DLL/NuGet**; `User`, `Group` and `Membership` source files are not required.

## Mandatory reading order

1. [`AGENTS.md`](AGENTS.md) and [`PORTABLE_CORE_CONTRACT.md`](PORTABLE_CORE_CONTRACT.md).
2. [`REFERENCE_CATALOG.md`](REFERENCE_CATALOG.md) to select an operation and its included sample.
3. The task-specific [`DOMAIN_CONTRACT.md`](DOMAIN_CONTRACT.md), [`ENTITY_CONTRACT.md`](ENTITY_CONTRACT.md) or [`VISUALIZATION_CONTRACT.md`](VISUALIZATION_CONTRACT.md).
4. [`UI_COMPOSITION_CONTRACT.md`](UI_COMPOSITION_CONTRACT.md) for UI and [`INPUT_COMPONENTS_CONTRACT.md`](INPUT_COMPONENTS_CONTRACT.md) for forms.
5. Local template source code under `Entity*Template/` plus `TEMPLATE_STATUS.md` for limitations. Verify compiled Core's **public** API signatures; do not require its source tree.

## Priority and availability

Small entity scaffolds put model, service and resolver C# files directly beside
their project files; UI retains `Pages` and `Components`. See the source-layout
rules in [DOMAIN_CONTRACT.md](DOMAIN_CONTRACT.md). Their source-checkout project
references use `Core/Contracts/Core.Contracts.csproj`; binary-only consumers
continue to resolve the same `Core.Contracts` assembly.

The above normative files take precedence over older per-template readmes. The package's public APIs are the authority for exact signatures. Historical implementations in `Core/User`, `Core/Group`, `Core/Membership` are **optional design provenance** only. If the public API does not match an example, report the version mismatch instead of fabricating wrappers.

**Known limits:** `ApplicationTemplate` is an older single-project scaffold; it does not yet generate a conforming four-project domain. Scalar field controls such as `TextInput` are not guaranteed to exist in Core. No untested scaffold may be described as complete or compiled. See `TEMPLATE_STATUS.md`.


## Endpoint metadata requirements (v140)

For resolver generation, first read [`ENDPOINT_CONTRACT.md`](ENDPOINT_CONTRACT.md) and the self-contained `EntityResolversTemplate/EntityNameEndpointExtensions.cs`. Use canonical routes, `WithName`, `WithDescription`, `WithTags`, `ApplyViewModel` and `ApplyJsonModel`; verify Swagger metadata. Core/User and Core/Group source code is **not required**.
