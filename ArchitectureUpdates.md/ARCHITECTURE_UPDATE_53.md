# Architecture Update 53 — Form designer, test mode and reusable subtree library

This update introduces the first functional designer for draft `FormVersion` trees.

## Designer

Canonical routes:

- `GET /digital-processes/FormVersionResponse/designer/{id}`
- `GET /digital-processes/FormVersionResponse/test/{id}`

The designer is deliberately server-rendered and does not depend on drag-and-drop JavaScript. It offers:

- hierarchical structure navigation,
- runtime-like form canvas,
- add root/child node,
- edit key/label/description/cardinality/configuration JSON,
- move node to another container and update materialized subtree paths,
- reusable subsection library,
- deep-clone import with optional key prefix.

Creating a `FormVersion` from the HTML form now redirects directly to its designer. The standard `FormVersion` detail card also exposes Designer/Test buttons.

## Test mode

Test mode renders the same registered node renderers used by the runtime form UI. Inputs are intentionally non-persistent dummy browser values: testing does not create a `Submission` or `SubmissionNode` rows.

This establishes the intended principle:

`Design structure -> Test using runtime renderers -> Runtime Submission`

rather than maintaining a separate preview renderer.

## Reusable component library

No parallel template tables were introduced. Any readable node whose `FormNodeType.can_have_children` is true in another `FormVersion` can currently act as a reusable section/subsection.

`ImportSubtreeAsync`:

1. reads the source subtree through RBAC,
2. validates the target draft/container,
3. deep-clones all nodes with new IDs,
4. rebuilds parent IDs and materialized paths,
5. validates target key collisions,
6. optionally prefixes imported keys,
7. inherits the target container RBAC scope.

The import is a copy, not a live reference. This keeps published form versions deterministic and lets a later component-catalog/versioning layer be added without changing the core clone operation.

## Domain service additions

`IFormNodeService` now contains:

- `GetDesignerAsync`
- `UpdateDefinitionAsync`
- `ImportSubtreeAsync`

Node creation also rejects duplicate keys inside one `FormVersion`.

## Deliberate limitations of this first designer

- no drag-and-drop yet,
- no physical component/catalog entity yet; readable sections from other versions are the initial library,
- imported nodes inherit the target security scope; source RBAC is never copied,
- test values are browser-only and reset on reload,
- no delete operation was added in this update; lifecycle/soft-delete semantics should be designed consistently rather than added ad hoc.

The next useful refinements are type-specific property editors derived from `configuration_schema_json`, soft-delete/restore for draft nodes, and explicit component-library metadata/versioning.
