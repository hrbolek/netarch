using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class DigitalProcessEndpointExtensions
{
    public static IEndpointRouteBuilder MapDigitalProcessEndpoints(this IEndpointRouteBuilder endpoints, string prefix = "/digital-processes")
    {
        DigitalProcessCreateEndpoints.Map(endpoints, prefix);
        MapReadOnly<FormDefinitionModel,FormDefinitionResponse,IFormDefinitionService,DigitalProcessModelPage<FormDefinitionResponse>,DigitalProcessListPage<FormDefinitionResponse>>(endpoints,prefix,"FormDefinitionResponse","FormDefinition");
        MapReadOnly<FormVersionModel,FormVersionResponse,IFormVersionService,DigitalProcessModelPage<FormVersionResponse>,DigitalProcessListPage<FormVersionResponse>>(endpoints,prefix,"FormVersionResponse","FormVersion");
        MapReadOnly<FormNodeTypeModel,FormNodeTypeResponse,IFormNodeTypeService,DigitalProcessModelPage<FormNodeTypeResponse>,DigitalProcessListPage<FormNodeTypeResponse>>(endpoints,prefix,"FormNodeTypeResponse","FormNodeType");
        MapReadOnly<FormNodeModel,FormNodeResponse,IFormNodeService,DigitalProcessModelPage<FormNodeResponse>,DigitalProcessListPage<FormNodeResponse>>(endpoints,prefix,"FormNodeResponse","FormNode");
        MapReadOnly<WorkflowDefinitionModel,WorkflowDefinitionResponse,IWorkflowDefinitionService,DigitalProcessModelPage<WorkflowDefinitionResponse>,DigitalProcessListPage<WorkflowDefinitionResponse>>(endpoints,prefix,"WorkflowDefinitionResponse","WorkflowDefinition");
        MapReadOnly<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService,DigitalProcessModelPage<WorkflowVersionResponse>,DigitalProcessListPage<WorkflowVersionResponse>>(endpoints,prefix,"WorkflowVersionResponse","WorkflowVersion");
        MapReadOnly<WorkflowStateModel,WorkflowStateResponse,IWorkflowStateService,DigitalProcessModelPage<WorkflowStateResponse>,DigitalProcessListPage<WorkflowStateResponse>>(endpoints,prefix,"WorkflowStateResponse","WorkflowState");
        MapReadOnly<WorkflowStateAccessRuleModel,WorkflowStateAccessRuleResponse,IWorkflowStateAccessRuleService,DigitalProcessModelPage<WorkflowStateAccessRuleResponse>,DigitalProcessListPage<WorkflowStateAccessRuleResponse>>(endpoints,prefix,"WorkflowStateAccessRuleResponse","WorkflowStateAccessRule");
        MapReadOnly<WorkflowTransitionModel,WorkflowTransitionResponse,IWorkflowTransitionService,DigitalProcessModelPage<WorkflowTransitionResponse>,DigitalProcessListPage<WorkflowTransitionResponse>>(endpoints,prefix,"WorkflowTransitionResponse","WorkflowTransition");
        MapReadOnly<ProcessDefinitionModel,ProcessDefinitionResponse,IProcessDefinitionService,DigitalProcessModelPage<ProcessDefinitionResponse>,DigitalProcessListPage<ProcessDefinitionResponse>>(endpoints,prefix,"ProcessDefinitionResponse","ProcessDefinition");
        MapReadOnly<ProcessDefinitionVersionModel,ProcessDefinitionVersionResponse,IProcessDefinitionVersionService,DigitalProcessModelPage<ProcessDefinitionVersionResponse>,DigitalProcessListPage<ProcessDefinitionVersionResponse>>(endpoints,prefix,"ProcessDefinitionVersionResponse","ProcessDefinitionVersion");
        MapReadOnly<RequestModel,RequestResponse,IRequestService,DigitalProcessModelPage<RequestResponse>,DigitalProcessListPage<RequestResponse>>(endpoints,prefix,"RequestResponse","Request");
        MapReadOnly<SubmissionModel,SubmissionResponse,ISubmissionService,DigitalProcessModelPage<SubmissionResponse>,DigitalProcessListPage<SubmissionResponse>>(endpoints,prefix,"SubmissionResponse","Submission");
        MapReadOnly<SubmissionNodeModel,SubmissionNodeResponse,ISubmissionNodeService,DigitalProcessModelPage<SubmissionNodeResponse>,DigitalProcessListPage<SubmissionNodeResponse>>(endpoints,prefix,"SubmissionNodeResponse","SubmissionNode");
        MapReadOnly<RequestTransitionHistoryModel,RequestTransitionHistoryResponse,IRequestTransitionHistoryService,DigitalProcessModelPage<RequestTransitionHistoryResponse>,DigitalProcessListPage<RequestTransitionHistoryResponse>>(endpoints,prefix,"RequestTransitionHistoryResponse","RequestTransitionHistory");

        var forms=endpoints.MapGroup(prefix+"/FormDefinitionResponse").WithTags("DigitalProcesses.Forms");
        forms.MapPost("/create",(CreateFormDefinitionRequest r,IFormDefinitionService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<FormDefinitionResponse>,FormDefinitionResponse>()
            .WithDescription("""
                Creates a new form definition.

                A form definition is the stable identity of a form. Editable form structure
                is stored in versioned form definitions and can later be published.
                """);
        var formVersions=endpoints.MapGroup(prefix+"/FormVersionResponse").WithTags("DigitalProcesses.Forms");
        formVersions.MapPost("/create",(CreateFormVersionRequest r,IFormVersionService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<FormVersionResponse>,FormVersionResponse>()
            .WithDescription("""
                Creates a new draft version of an existing form definition.

                Form nodes can be edited while the version is in draft state. Published
                versions are immutable and can be referenced by process definitions.
                """);
        formVersions.MapPost("/publish/{id:guid}",(Guid id,IFormVersionService s,CancellationToken ct)=>s.PublishAsync(id,ct))
            .ApplyJsonModel<DigitalProcessModelPage<FormVersionResponse>,FormVersionResponse>()
            .WithDescription("""
                Publishes a draft form version.

                Publishing freezes the structural definition so existing and future request
                submissions can safely reference the same immutable form version.
                """);
        formVersions.MapGet("/tree/{id:guid}",(Guid id,IFormNodeService s,CancellationToken ct)=>s.GetTreeAsync(id,ct))
            .ApplyViewModel<FormTreePage,FormTreeResponse>()
            .WithDescription("""
                Returns the complete form-definition tree for a form version.

                The tree is composed of homogeneous FormNode entities and preserves their
                materialized paths, occurrence constraints and renderer configuration.
                """);
        var nodeTypes=endpoints.MapGroup(prefix+"/FormNodeTypeResponse").WithTags("DigitalProcesses.Forms");
        nodeTypes.MapPost("/create",(CreateFormNodeTypeRequest r,IFormNodeTypeService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<FormNodeTypeResponse>,FormNodeTypeResponse>()
            .WithDescription("""
                Creates a form-node type.

                Node types define capabilities and renderer keys used by the low-code form
                renderer. New visual representations can be introduced without changing the form engine.
                """);
        var nodes=endpoints.MapGroup(prefix+"/FormNodeResponse").WithTags("DigitalProcesses.Forms");
        nodes.MapPost("/create",(CreateFormNodeRequest r,IFormNodeService s,CancellationToken ct)=>s.CreateNodeAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<FormNodeResponse>,FormNodeResponse>()
            .WithDescription("""
                Adds a node to a draft form-version tree.

                The service computes the materialized path from the parent node and the new
                node identifier. Client supplied paths are not accepted as authoritative data.
                """);
        nodes.MapPut("/move/{id:guid}",(Guid id,MoveFormNodeRequest r,IFormNodeService s,CancellationToken ct)=>s.MoveAsync(id,r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<FormNodeResponse>,FormNodeResponse>()
            .WithDescription("""
                Moves a form node to a new parent in a draft form version.

                The node and all descendants receive updated materialized paths so subtree
                queries remain consistent after the move.
                """);
        nodes.MapGet("/subtree/{id:guid}",async(Guid id,IFormNodeService s,CancellationToken ct)=>(await s.GetSubtreeAsync(id,ct)).ToArray())
            .ApplyJsonModel<DigitalProcessListPage<FormNodeResponse>,FormNodeResponse[]>()
            .WithDescription("""
                Returns a form node and its complete descendant subtree using the materialized path.
                """);

        var workflows=endpoints.MapGroup(prefix+"/WorkflowDefinitionResponse").WithTags("DigitalProcesses.Workflow");
        workflows.MapPost("/create",(CreateWorkflowDefinitionRequest r,IWorkflowDefinitionService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowDefinitionResponse>,WorkflowDefinitionResponse>()
            .WithDescription("""
                Creates a new workflow definition that can contain multiple immutable versions.
                """);
        var workflowVersions=endpoints.MapGroup(prefix+"/WorkflowVersionResponse").WithTags("DigitalProcesses.Workflow");
        workflowVersions.MapPost("/create",(CreateWorkflowVersionRequest r,IWorkflowVersionService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowVersionResponse>,WorkflowVersionResponse>()
            .WithDescription("""
                Creates a draft workflow version containing states, transitions and access templates.
                """);
        workflowVersions.MapPost("/publish/{id:guid}",(Guid id,IWorkflowVersionService s,CancellationToken ct)=>s.PublishAsync(id,ct))
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowVersionResponse>,WorkflowVersionResponse>()
            .WithDescription("""
                Publishes a workflow version and freezes its state graph for use by process versions.
                """);
        var states=endpoints.MapGroup(prefix+"/WorkflowStateResponse").WithTags("DigitalProcesses.Workflow");
        states.MapPost("/create",(CreateWorkflowStateRequest r,IWorkflowStateService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowStateResponse>,WorkflowStateResponse>()
            .WithDescription("""
                Adds a state to a draft workflow version.
                """);
        var accessRules=endpoints.MapGroup(prefix+"/WorkflowStateAccessRuleResponse").WithTags("DigitalProcesses.Workflow");
        accessRules.MapPost("/create",(CreateWorkflowStateAccessRuleRequest r,IWorkflowStateAccessRuleService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowStateAccessRuleResponse>,WorkflowStateAccessRuleResponse>()
            .WithDescription("""
                Adds a declarative RBAC access rule to a workflow state.

                When a request enters the state, the rule is compiled into ordinary RBAC
                AccessBindings for the request, submission or a form-node security scope.
                Runtime authorization still uses only EffectiveAccess.
                """);

        var transitions=endpoints.MapGroup(prefix+"/WorkflowTransitionResponse").WithTags("DigitalProcesses.Workflow");
        transitions.MapPost("/create",(CreateWorkflowTransitionRequest r,IWorkflowTransitionService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowTransitionResponse>,WorkflowTransitionResponse>()
            .WithDescription("""
                Adds a directed transition between two states in a draft workflow version.
                """);

        var definitions=endpoints.MapGroup(prefix+"/ProcessDefinitionResponse").WithTags("DigitalProcesses.Process");
        definitions.MapPost("/create",(CreateProcessDefinitionRequest r,IProcessDefinitionService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<ProcessDefinitionResponse>,ProcessDefinitionResponse>()
            .WithDescription("""
                Creates a process definition that owns the long-lived identity and RBAC scope of a digital process.
                """);
        var definitionVersions=endpoints.MapGroup(prefix+"/ProcessDefinitionVersionResponse").WithTags("DigitalProcesses.Process");
        definitionVersions.MapPost("/create",(CreateProcessDefinitionVersionRequest r,IProcessDefinitionVersionService s,CancellationToken ct)=>s.CreateAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<ProcessDefinitionVersionResponse>,ProcessDefinitionVersionResponse>()
            .WithDescription("""
                Creates a draft process-definition version connecting a form version with a workflow version.
                """);
        definitionVersions.MapPost("/publish/{id:guid}",(Guid id,IProcessDefinitionVersionService s,CancellationToken ct)=>s.PublishAsync(id,ct))
            .ApplyJsonModel<DigitalProcessModelPage<ProcessDefinitionVersionResponse>,ProcessDefinitionVersionResponse>()
            .WithDescription("""
                Publishes a process-definition version.

                Both referenced form and workflow versions must already be published before
                requests can be started from this process version.
                """);

        var requests=endpoints.MapGroup(prefix+"/RequestResponse").WithTags("DigitalProcesses.Runtime");
        requests.MapPost("/start",(StartRequestRequest r,IRequestService s,CancellationToken ct)=>s.StartAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<RequestResponse>,RequestResponse>()
            .WithDescription("""
                Starts a new request from a published process-definition version.

                The current actor is stored as the immutable request initiator. A request RBAC
                scope and the first active submission are created, mandatory form nodes are
                materialized, state access rules are compiled into RBAC bindings and optional
                prefilled data can become part of the initial submission.
                """);
        requests.MapPost("/transition/{id:guid}",(Guid id,TransitionRequestRequest r,IRequestService s,CancellationToken ct)=>s.TransitionAsync(id,r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<RequestResponse>,RequestResponse>()
            .WithDescription("""
                Performs an allowed workflow transition.

                The current submission is archived, its historical RBAC structure remains intact,
                a new submission tree with new node identifiers, materialized paths and RBAC scopes
                is created for the target state, transition history is recorded, state-specific
                RBAC AccessBindings are materialized, and the request is moved to the target state.
                """);
        requests.MapGet("/detail/{id:guid}",(Guid id,IRequestService s,CancellationToken ct)=>s.GetDetailAsync(id,ct))
            .ApplyViewModel<RequestDetailPage,RequestDetailResponse>()
            .WithDescription("""
                Returns a request together with its current workflow state, active submission and transition context.
                """);

        var submissions=endpoints.MapGroup(prefix+"/SubmissionResponse").WithTags("DigitalProcesses.Runtime");
        submissions.MapGet("/tree/{id:guid}",(Guid id,ISubmissionService s,CancellationToken ct)=>s.GetTreeAsync(id,ct))
            .ApplyViewModel<SubmissionTreePage,SubmissionTreeResponse>()
            .WithDescription("""
                Returns the authorized submission tree.

                Submission nodes are secured by their RBAC object identifiers; nodes for which
                the current actor lacks read permission are not returned as merely hidden UI data.
                """);
        var submissionNodes=endpoints.MapGroup(prefix+"/SubmissionNodeResponse").WithTags("DigitalProcesses.Runtime");
        submissionNodes.MapPost("/create",(CreateSubmissionNodeRequest r,ISubmissionNodeService s,CancellationToken ct)=>s.CreateNodeAsync(r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<SubmissionNodeResponse>,SubmissionNodeResponse>()
            .WithDescription("""
                Creates an additional runtime submission node, typically an occurrence of a repeatable form node.

                Nodes with their own security scope receive a new RBAC group and the current
                workflow state's access rules are compiled for that scope.
                """);
        submissionNodes.MapPut("/value/{id:guid}",(Guid id,SetSubmissionNodeValueRequest r,ISubmissionNodeService s,CancellationToken ct)=>s.SetValueAsync(id,r,ct))
            .ApplyJsonModel<DigitalProcessModelPage<SubmissionNodeResponse>,SubmissionNodeResponse>()
            .WithDescription("""
                Stores a value in an editable submission node.

                Archived submissions are immutable. Authorization is evaluated through the
                ordinary RBAC EffectiveAccess projection for the node's RBAC object.
                """);
        submissionNodes.MapGet("/subtree/{id:guid}",async(Guid id,ISubmissionNodeService s,CancellationToken ct)=>(await s.GetSubtreeAsync(id,ct)).ToArray())
            .ApplyJsonModel<DigitalProcessListPage<SubmissionNodeResponse>,SubmissionNodeResponse[]>()
            .WithDescription("""
                Returns an authorized runtime subtree rooted at the selected submission node.
                """);

        return endpoints;
    }

    private static void MapReadOnly<TDB,TDTO,TService,TView,TListView>(IEndpointRouteBuilder endpoints,string prefix,string route,string tag)
        where TDB:class,WebAppArchitecture.Base.Data.ISecuredEntityDBModel
        where TDTO:IEntityDTOModel<TDB,TDTO>
        where TService:IBaseService<TDB,TDTO>
        where TView:IComponent
        where TListView:IComponent
    {
        var group=endpoints.MapGroup(prefix+"/"+route).WithTags("DigitalProcesses."+tag);
        group.MapGet("/view/{id:guid}",(Guid id,HttpContext http,TService service,CancellationToken ct)=>BaseResolvers.Read<TDB,TDTO,TService>(id,http,service,ct))
            .ApplyViewModel<TView,TDTO>()
            .WithDescription("""
                Returns one entity by identifier after applying the standard RBAC read filter.
                The endpoint can render HTML or return JSON according to content negotiation.
                """);
        group.MapGet("/list",(HttpContext http,TService service,CancellationToken ct)=>BaseResolvers.List<TDB,TDTO,TService>(http,service,ct))
            .ApplyViewModel<TListView,TDTO[]>()
            .WithDescription("""
                Returns entities visible to the current actor through the standard RBAC list operation.
                The endpoint can render HTML or return JSON according to content negotiation.
                """);
        group.MapGet("/link/{id:guid}",(Guid id,HttpContext http,TService service,CancellationToken ct)=>BaseResolvers.Link<TDB,TDTO,TService>(id,http,service,ct))
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy links.
                HTML requests render an EntityLink fragment while JSON requests return the reference DTO.
                """);
    }
}
