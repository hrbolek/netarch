# Update 65 — sjednocení vizuálních reprezentací

Porovnání s Group.UI: GroupViewListPage používá PageViewList a GroupMediumCard; GroupViewPage používá PageView a GroupLargeCard; LargeCard má MediumCard, EntityLinks a EntityPermissions vlevo a specifický ChildContent uprostřed.

DigitalProcesses: list používá PageViewList pro zobrazení entit; view používá PageView s LargeCard; LargeCard doplňuje odkazy a RBAC přehled; MediumCard uvádí základní atributy. Detail neuvádí jen JSON: atributy jsou čitelné a původní JSON je skrytý pod diagnostics. Doménové editory zůstávají zachovány. Bez změn URI, resolverů a API kontraktů.

Poznámka: generické PageView s TComponent vyžaduje ověření kompilace na cílové .NET SDK.

## Entity-based UI layout

UI source files and namespaces are grouped by entity, then by role:

```text
DigitalProcesses.UI/
  FormDefinition/Pages/
  FormVersion/Pages/
  FormNode/Components/
  FormNode/Renderers/
  ProcessDefinition/Pages/
  ProcessDefinitionVersion/Pages/
  WorkflowDefinition/Pages/
  WorkflowVersion/Pages/
  WorkflowVersion/Components/
  Request/Pages/
  Submission/Pages/
  SubmissionNode/Components/
  Shared/Components/
  Shared/Routing/
```

Only populated directories are created. All list, model and create pages are
concrete components in their entity's Pages directory, and resolvers bind directly
to these types. Shared has no Pages directory. Low-level form controls, common
form contracts and the versions table belong in Shared/Components. List pages reuse
the base PageViewList; detail pages reuse PageView and entity-specific cards.
Titles are declared by each entity page rather than selected by a shared type
switch. The workflow graph and geometry belong to
WorkflowVersion; submission-value rendering belongs to SubmissionNode. FormNode
owns the editor components, section/table components, concrete renderers and their
registry. Static assets remain under wwwroot to preserve existing asset URLs.
Resolver bindings, dependency registration and tests use the entity namespaces.
HTTP routes and rendering behavior are unchanged.

Process and workflow definition resolvers bind directly to
DefinitionProcessVersionPage and DefinitionWorkflowVersionPage, respectively.
Each page has its own typed model; there is no DefinitionVersionsPage dispatcher
or shared model with alternative definition DTOs. Kind remains a read-only JSON
discriminator on the domain models for compatibility, not a UI routing input.
DefinitionVersionsTable receives only its display data and definition URL prefix.

## FormVersion designer

Template management uses `/digital-processes/FormVersionResponse/templates/{id}`.
The former `/digital-processes/form-versions/{id}/templates` URI redirects to it.
Its FormVersionResponse includes the optional `templates` collection, populated
by FormVersionService only after authorization on the version. Other version
projections leave this property null and omit it from JSON. Initial template
links render on the server; uploads/generation refresh through the existing API.
The root-permission administration page accepts the standard IsFragment parameter
used by ApplyViewModel for both full HTML and fragment responses.

`FormVersion/Pages/FormVersionDesignerPage.razor` replaces FormDesignerPage.
The page composes `FormVersionDesignerHeader`, `FormVersionDesignerWorkspace`
and `FormVersionDesignerPreview`; the workspace uses version-owned AI/template
cards and entity-owned node components. `FormNodeStructureCard`,
`FormNodeLibraryCard`, `FormNodeAddCard` and `FormNodeEditorDialogs` reside in
FormNode/Components. Shared tree/path/container calculations belong to
`FormNodeEditorView`, not the page.

The 543-line monolith is reduced to a page of about 50 lines. AI interaction,
node-action menu handling and preview/repeated-value interaction scripts are
separate, versioned assets in wwwroot, without changing their request or DOM
contracts. Design/test URLs, published-version behavior, node-subtree editing,
the insertion wizard and the FormDesignerPageResponse JSON contract are unchanged.
Both FormVersion and FormNode editor resolvers use FormVersionDesignerPage.

## Entity-owned cards

`RequestResponse/view/{id}` renders the runtime Request workspace, not just the
technical entity card. RequestService.GetViewAsync supplies an optional workspace
on RequestResponse with the active submission, authorized form definitions,
workflow states/transitions, ordered history and per-node edit permissions.
RequestWorkspace is shared with the process test page: values use the existing
secured SubmissionNode value endpoint; transitions save editable fields first,
then call the existing Request transition command. The request view posts
`return_to=request` to return to its canonical URI; process tests keep their
existing return target. Read-only/archived nodes have disabled inputs and actors
without the required Request/Submission/group Update grants get no transition
buttons. Server-side mutation authorization remains authoritative.

Each displayed entity owns its cards under Entity/Components.
FormDefinition uses a single MediumCard with explicit Attribute components for
all fields, including RBAC/audit metadata, and expandable JSON diagnostics;
its former ModelCard is removed. LargeCard renders that card once in its main
content, or alongside custom ChildContent in its sidebar. Lists use the same card.
Other entities still own separate MediumCard, LargeCard and ModelCard components.
The nine entities with related-entity links also own a
ForeignKeyLinks component. These components use concrete response DTOs and
explicit properties: no runtime reflection or switches on entity types select
fields, navigation, actions or relationships. Large cards retain their typed
ChildContent slots and RBAC panels, and model cards retain JSON diagnostics.
The common Core/Base card components still provide the shared layout.

Shared/Components contains seven genuinely reusable building blocks:
CreateFormField, CreateFormFields, DefinitionVersionItem, DefinitionVersionsTable,
DigitalProcessCreateField, DigitalProcessCreatePageModel and DigitalProcessSelectOption.

## Entity-owned creation

Digital Processes > Administration > Creation permissions and roles links to the
existing secured `/architecture/root-permissions` page. It exposes the four
creation scopes (FormDefinition, WorkflowDefinition, ProcessDefinition and
FormNodeType) through the standard EntityPermissions role-management components.
The page requires Read on Root; each scope's permissions and role actions retain
their own Read/Create checks. Creation-scope roles govern permission to create,
while roles on an existing object's rbacobject_id remain managed from its detail.
Contained creation uses the parent scope. Clients still cannot select an arbitrary
rbacobject_id, and neither authorization rules nor the database schema change.

Each of the 12 create pages owns its heading, guidance and navigation. Its entity
also owns a CreateCard with an explicit field list and a CreateForm configuration.
Resolvers delegate configuration to that entity's UI instead of defining the form.
DigitalProcessCreateContent and DigitalProcessMutationCard have been removed.
Only single-field rendering and field-construction helpers remain shared.
DigitalProcessCreatePageModel remains the existing JSON configuration contract;
field names, required flags, defaults, limits and dependent lookup settings are
preserved. POST parsing stays in Resolvers/Shared/CreateFormValues.

## Consistent cross-layer ownership

Models, Services and Resolvers use the same entity directory names as UI.
Namespaces mirror their directories, for example:

```text
DigitalProcesses.Models/ProcessDefinition/ProcessDefinitionModel.cs
DigitalProcesses.Models/ProcessDefinition/ProcessDefinitionResponse.cs
DigitalProcesses.Models/ProcessDefinition/CreateProcessDefinitionRequest.cs
DigitalProcesses.Services/ProcessDefinition/IProcessDefinitionService.cs
DigitalProcesses.Services/ProcessDefinition/ProcessDefinitionService.cs
DigitalProcesses.Resolvers/ProcessDefinition/ProcessDefinitionResolvers.cs
DigitalProcesses.UI/ProcessDefinition/Pages/ProcessDefinitionCreatePage.razor
DigitalProcesses.UI/ProcessDefinition/Components/ProcessDefinitionCreateCard.razor
```

The extra Models/Models, Services/Services, Resolvers/Resolvers and DatabaseModels
directories are gone. Mixed DTO and auxiliary-service contract files are split
by entity. DocumentTemplate, DocumentTemplateRevision, TemplateFieldBinding and
FormNodeApplicantAccess have explicit model ownership too. Backend transport and
integration plumbing reside in Services/Infrastructure. Shared contains only
cross-entity helpers, not entity-specific management pages.

The former DigitalProcessesManagementService and its resolver are removed:
applicant-matrix reads belong to ProcessDefinitionVersionService and its permissions
resolver; template-page reads belong to FormVersionService and its templates
resolver. Existing read/update checks and draft-only editability are unchanged.
Trusted request provisioning belongs to Request; trusted snapshot reading belongs
to Submission. Their transaction and authorization behavior is unchanged.
The former BulkTreeService is also removed: the complete batch algorithms now
reside directly in FormVersionService.Tree and SubmissionService.Values. AI form
approval delegates to IFormVersionService rather than a parallel persistence
helper. Authorization checks, staging order and transaction boundaries are
preserved. Starter-content staging is split into FormVersionDefaultContent and
WorkflowVersionDefaultContent, retaining the explicit trusted bootstrap path.

## Auxiliary-service composition

Only canonical entity services derive from SecuredBaseService: one per secured
model, named EntityService. All eight formerly inheriting auxiliary services now
use composition:

| Auxiliary service | Entity-service boundary |
| --- | --- |
| FormSearchService | IFormNodeService and IFormNodeTypeService; approved DTO reads |
| FormComponentCatalogService | IFormNodeTypeService; approved DTO reads |
| FormDesignAiService | IFormVersionService, IFormNodeService and IFormNodeTypeService; approved DTO reads and secured tree writes |
| FormNodeInsertionService | IFormNodeService; insertion, detached draft preparation and library queries |
| FormDocumentService | ISubmissionService; authorized schema/document projection |
| ApplicantPolicyService | IFormNodeApplicantAccessService; policy reads and atomic writes |
| RtfTemplateService | IFormVersionService; template and revision operations authorized against their owning form version |
| DigitalProcessActionService&lt;TEntity&gt; | IBaseService&lt;TEntity&gt;; minimal identity DTO read |

Helpers do not receive ISecurityLayer, IDBLayer, DbContext or RtfTemplateStore.
They never obtain raw database models from entity-service APIs. Read-only
orchestration explicitly preserves Read (not List) permission filtering; AI
approval still requires Update on the version.

FormNodeService.Insertion owns insertion and hidden-sibling invariant checks.
SubmissionService.Documents owns schema and submission-document queries.
FormVersionService.Templates owns the unchanged template transaction blocks and
uses RtfTemplateStore for staging. DocumentTemplate, DocumentTemplateRevision and
TemplateFieldBinding are not independently secured database models: their CRUD
is authorized through the owning FormVersion, without inventing RBAC columns or
changing tables. FormNodeApplicantAccessService now provides that secured
entity's canonical service.

DI registers IBaseService&lt;EntityModel&gt; as an alias to the existing named,
scoped entity service, not a second instance. All participating services retain
the shared request-scoped security layer, EF context and transaction.

ServiceCompositionTests enforces unique canonical inheritance, helper dependency
boundaries, successful DI resolution and scoped alias identity.
EndpointLayeringTests permits composition only through directly injected secured
entity contracts, while continuing to prohibit persistence/security-layer
injection into resolvers. ComposedServiceBehaviorTests covers template revisions,
invalid binding rejection, cascade deletion, denied actors, draft-only applicant
policy writes and AI approval. Existing insertion/search tests continue to cover
hidden rows, audit values, rollback and HTTP contracts.
An explicit Read-only RBAC fixture verifies that catalog reads do not silently
switch to List permission and AI approval cannot proceed without Update.
The AI proposal test uses an in-process HTTP handler and never calls an external
agent.

Database CLR property declarations and complete EF mapping expressions are
unchanged, apart from namespaces/imports. Original-namespace and reorganized
models produce the same PostgreSQL creation-script SHA-256:
`18D2C43D7D88274A710450978D374181450D49B2A8DE257AF97C461ACDAA4CA5`.
EntityOrganizationTests locks the layout and this schema fingerprint without
opening a database connection. No database or migration changes are required.
