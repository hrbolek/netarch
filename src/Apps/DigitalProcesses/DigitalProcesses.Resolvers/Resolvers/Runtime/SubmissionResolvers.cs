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

public static class SubmissionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, ISubmissionService service, CancellationToken ct) => BaseResolvers.Read<SubmissionModel,SubmissionResponse,ISubmissionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, ISubmissionService service, CancellationToken ct) => BaseResolvers.List<SubmissionModel,SubmissionResponse,ISubmissionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, ISubmissionService service, CancellationToken ct) => BaseResolvers.Link<SubmissionModel,SubmissionResponse,ISubmissionService>(id,http,service,ct);
    public static Task<SubmissionTreeResponse> Tree(Guid id, ISubmissionService service, CancellationToken ct) => service.GetTreeAsync(id, ct);

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/SubmissionResponse").WithTags("DigitalProcesses.Runtime");
        group.MapActions<SubmissionModel>("SubmissionResponse");
        group.MapGet("/view/{id:guid}", SubmissionResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<SubmissionResponse>,SubmissionResponse>()
            .WithDescription("""
                Returns one Submission visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", SubmissionResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<SubmissionResponse>,SubmissionResponse[]>()
            .WithDescription("""
                Returns Submission entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", SubmissionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);
        group.MapGet("/tree/{id:guid}", SubmissionResolvers.Tree)
            .ApplyViewModel<SubmissionTreePage,SubmissionTreeResponse>()
            .WithDescription("""
                Returns the authorized submission tree. Nodes without read permission are omitted before rendering or JSON serialization.
                """);
    }
}
