# Template status / known gaps (ZArchitecture 139)

| Scaffold | Current status | Main unresolved gap |
|---|---|---|
| ApplicationTemplate | LEGACY skeleton, not contract-compliant | One project; no complete UI, Create, tests; older registration guidance |
| EntityModelTemplate | Skeleton | `CreateRequest` accepts arbitrary `rbacobject_id` / Root default; not safe as prescribed Create |
| EntityServiceTemplate | Skeleton | No domain-owned `CreateContained`, no validation or synchronization rules |
| EntityResolversTemplate | Skeleton | No Create/link/actions or integrated HTML/JSON endpoint map; older route layout |
| EntityUITemplate | Reference-aligned UI scaffold, still incomplete | Needs wired create/update/resolver/menu; `APPLICATION_PREFIX` route must be replaced |

**Never claim `dotnet new architecture-app` produces a finished secure module.** The contracts set completion requirements. The local contracts and included template source are the required examples. Historical Core domain sources are optional and must never be required. Binary-only compile validation is not yet implemented. Build/template-generation tests are required before changing status to verified.

**Field input status (138):** `INPUT_COMPONENTS_CONTRACT.md` now documents existing Core `EntityLookupSelect`, actual Create/Update conventions and mandatory FK behavior. Shared scalar Input components are NOT implemented in Core; generated UI scalar form remains a documented skeleton. No build/runtime claim is made.

**Binary-only portability (139):** Added `PORTABLE_CORE_CONTRACT.md` and `REFERENCE_CATALOG.md`; all required references to Core domain source trees have been removed from normative generation instructions. Existing scaffold gaps are unchanged; packaged examples still require API version validation/build before claiming full compatibility.


### v140 resolver scope
`EntityResolversTemplate` now maps real read/list/edit/link/update/delete routes with metadata and Core representation extensions. Secure create, forms, actions, vector and integration tests remain incomplete; `ApplicationTemplate` is a legacy skeleton and must not be presented as the full canonical implementation.
