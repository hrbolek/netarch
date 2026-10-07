using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class SubmissionNodeResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, ISubmissionNodeService service, CancellationToken ct) => BaseResolvers.Read<SubmissionNodeModel,SubmissionNodeResponse,ISubmissionNodeService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, ISubmissionNodeService service, CancellationToken ct) => BaseResolvers.List<SubmissionNodeModel,SubmissionNodeResponse,ISubmissionNodeService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, ISubmissionNodeService service, CancellationToken ct) => BaseResolvers.Link<SubmissionNodeModel,SubmissionNodeResponse,ISubmissionNodeService>(id,http,service,ct);
    public static Task<SubmissionNodeResponse> CreateInSubmission(CreateSubmissionNodeRequest request, ISubmissionNodeService service, CancellationToken ct)
        => service.CreateInSubmissionAsync(request, ct);
    public static Task<SubmissionNodeResponse> CreateInNode(CreateSubmissionNodeRequest request, ISubmissionNodeService service, CancellationToken ct)
        => service.CreateInNodeAsync(request, ct);
    public static Task<SubmissionNodeResponse> SetValue(Guid id, SetSubmissionNodeValueRequest request, ISubmissionNodeService service, CancellationToken ct) => service.SetValueAsync(id, request, ct);
    public static async Task<SubmissionNodeResponse[]> Subtree(Guid id, ISubmissionNodeService service, CancellationToken ct)
        => (await service.GetSubtreeAsync(id, ct)).ToArray();

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/SubmissionNodeResponse").WithTags("DigitalProcesses.Runtime");
        group.MapGet("/view/{id:guid}", SubmissionNodeResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<SubmissionNodeResponse>,SubmissionNodeResponse>()
            .WithDescription("""
                Returns one SubmissionNode visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", SubmissionNodeResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<SubmissionNodeResponse>,SubmissionNodeResponse[]>()
            .WithDescription("""
                Returns SubmissionNode entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", SubmissionNodeResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);
        group.MapPost("/create-in-submission", SubmissionNodeResolvers.CreateInSubmission)
            .ApplyJsonModel<DigitalProcessModelPage<SubmissionNodeResponse>,SubmissionNodeResponse>()
            .WithDescription("""
                Creates a root runtime SubmissionNode inside submission_id. Create permission is evaluated on the existing Submission RBAC scope. The form definition must identify a root FormNode; rbacobject_id cannot be supplied by the client.
                """);
        group.MapPost("/create-in-node", SubmissionNodeResolvers.CreateInNode)
            .ApplyJsonModel<DigitalProcessModelPage<SubmissionNodeResponse>,SubmissionNodeResponse>()
            .WithDescription("""
                Creates a child runtime SubmissionNode below parent_id. Create permission is evaluated on the parent SubmissionNode RBAC scope. The service enforces submission ownership, form-tree structure and occurrence limits; rbacobject_id cannot be supplied by the client.
                """);
        group.MapPut("/value/{id:guid}", SubmissionNodeResolvers.SetValue)
            .ApplyJsonModel<DigitalProcessModelPage<SubmissionNodeResponse>,SubmissionNodeResponse>()
            .WithDescription("""
                Stores a value in an editable submission node; archived submissions remain immutable.
                """);
        group.MapGet("/subtree/{id:guid}", SubmissionNodeResolvers.Subtree)
            .ApplyJsonModel<DigitalProcessListPage<SubmissionNodeResponse>,SubmissionNodeResponse[]>()
            .WithDescription("""
                Returns the authorized runtime subtree rooted at the selected submission node.
                """);
    }
}
