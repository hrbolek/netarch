# Portable Core dependency contract (NORMATIVE)

## Guaranteed mode: binary-only Core

All instructions in `/templates/` **MUST work when the implementation of Core/User, Core/Group and Core/Membership is not available** and the application references only compiled .NET assemblies or NuGet packages. Those modules inspired the contracts but are **not runtime/build/generation prerequisites**. Never require reading, copying, modifying or including their `.cs`/`.razor` sources.

**Authority in binary-only distribution:** (1) contracts and self-contained examples in `/templates/`; (2) public compile-time APIs exposed by referenced assemblies and their XML documentation; (3) versions/compatibility recorded in the consumer's project; (4) project-local code. Historical Core source, when available, is optional background only and must not override the published API or the contracts.

## Required Core API inventory

Before generating, the agent must check the *public* API (via XML docs, IDE metadata navigation, reflection, or a small compile-time probe) for all types it intends to use. Verify namespaces, type and generic constraints, Razor component parameter names/types, extension method signatures, DI registrations, and endpoint filter behavior. **Do not infer a signature from a historical implementation.**

Minimal lookup catalog (API availability is package-version-dependent):

| Concern | Candidate API | Required verification |
|---|---|---|
| Composition | `Page`, `PageViewList`, `PageView`, `PageEdit`, `ViewVector` | Generic arguments, `Model`, `IsFragment` and content slots |
| Entity display | `MediumCard`, `LargeCard`, `EntityLink`, `LazyEntityLink` | Model type, link and slot properties |
| Entity permissions | `EntityPermissions`, `EntityLinks` | Parameter types and effective permission shape |
| Input relations | `EntityLookupSelect` | `Name`, `Label`, `ListUrl`, `SearchFields`, `InitialId`, `InitialLabel`, `Required`, paging |
| HTTP mapping | `ApplyViewModel`, `ApplyJsonModel`, endpoint map methods | Method namespaces, generic constraints, MVC/Razor registration |
| Security | secured service APIs, `CreationScopeGuard` where published | Scope checks, transactions and batch authorization API |

If an API is missing in the supplied binary, **stop inventing component names**: report the mismatch, use a verified supported alternative, or explicitly request a versioned Core dependency update. Never create a same-named approximation inside a domain.

## Distribution acceptance

- `/templates/` plus documented binary dependencies is sufficient to find the prescribed architecture, UI design and field behavior; no repository-relative Core source is required.
- A generated sample builds with *only binary references* to Core assemblies, aside from the new domain's own sources.
- The templates state version constraints and do not promise compilation against unspecified older DLLs.
- Tests must run with `src/Core/User`, `src/Core/Group` and `src/Core/Membership` **absent** from the consumer workspace.
- Templates use the real public APIs, not source-file paths, as the final signature authority.

## Illustrative composition (must adapt to installed public signatures)

```razor
@* List page — component names are contracts, verify generic parameters against DLL *@
<PageViewList TModel="EntityResponse" Model="@Model">
    @* Render each item with the domain EntityMediumCard in the list content slot. *@
</PageViewList>
```

For concrete Razor syntax, consult the files in `EntityUITemplate/Pages` and `EntityUITemplate/Components`. For FK selector input use `INPUT_COMPONENTS_CONTRACT.md`. Those checked-in templates, not an external Core source tree, are the example payloads.


## Endpoint metadata requirements (v140)

For resolver generation, first read [`ENDPOINT_CONTRACT.md`](ENDPOINT_CONTRACT.md) and the self-contained `EntityResolversTemplate/EntityNameEndpointExtensions.cs`. Use canonical routes, `WithName`, `WithDescription`, `WithTags`, `ApplyViewModel` and `ApplyJsonModel`; verify Swagger metadata. Core/User and Core/Group source code is **not required**.
