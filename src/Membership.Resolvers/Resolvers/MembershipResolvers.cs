using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Membership.UI.Pages;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Resolvers;

public static class MembershipResolvers
{
    public static async Task<IResult> CreatePage(
        HttpContext httpContext,
        IRbacService rbacService,
        ICurrentActorProvider actorProvider,
        CancellationToken cancellationToken)
    {
        if (actorProvider.TryGetActorUserId(httpContext) is null)
            return Results.Unauthorized();

        var allowed = await rbacService.IsCurrentActorAllowedAsync(
            httpContext,
            RbacWellKnown.RootSecurityObjectId,
            RbacOperations.Create,
            cancellationToken);

        if (!allowed)
            return Results.Forbid();

        return new RazorComponentResult<MembershipCreatePage>();
    }

    public static async Task<IResult> Create(MembershipCreateRequest request, HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
    {
        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actorUserId = actorProvider.TryGetActorUserId(httpContext);
        if (actorUserId is null)
            return Results.Unauthorized();

        var now = DateTimeOffset.UtcNow;
        var stampedRequest = request with
        {
            created_at = now,
            updated_at = now,
            created_by = actorUserId,
            updated_by = actorUserId
        };

        return await BaseResolvers.Create<MembershipModel, MembershipResponse, MembershipCreateRequest, IMembershipService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> CreateForm(
        HttpContext httpContext,
        IMembershipService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);

        if (!Guid.TryParse(form["group_id"].ToString(), out var groupId))
            return Results.BadRequest(new { error = "Invalid group_id value." });

        if (!Guid.TryParse(form["user_id"].ToString(), out var userId))
            return Results.BadRequest(new { error = "Invalid user_id value." });

        var validFromRaw = form["valid_from"].ToString();
        var validFrom = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(validFromRaw) && !DateTimeOffset.TryParse(validFromRaw, out validFrom))
            return Results.BadRequest(new { error = "Invalid valid_from value." });

        DateTimeOffset? validTo = null;
        var validToRaw = form["valid_to"].ToString();
        if (!string.IsNullOrWhiteSpace(validToRaw))
        {
            if (!DateTimeOffset.TryParse(validToRaw, out var parsedValidTo))
                return Results.BadRequest(new { error = "Invalid valid_to value." });

            validTo = parsedValidTo;
        }

        var result = await Create(
            new MembershipCreateRequest(groupId, userId, validFrom, validTo),
            httpContext,
            service,
            cancellationToken);

        if (result is IModelResult modelResult && modelResult.Model is MembershipResponse created)
            return Results.Redirect($"/architecture/MembershipResponse/edit/{created.id}");

        return result;
    }

    public static Task<IResult> Read(Guid id, HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<MembershipModel, MembershipResponse, IMembershipService>(id, httpContext, service, cancellationToken);

    public static Task<IResult> List(HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
        => BaseResolvers.List<MembershipModel, MembershipResponse, IMembershipService>(httpContext, service, cancellationToken);

    public static async Task<IResult> Update(MembershipUpdateRequest request, HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
    {
        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actorUserId = actorProvider.TryGetActorUserId(httpContext);
        if (actorUserId is null)
            return Results.Unauthorized();

        var stampedRequest = request with
        {
            updated_at = DateTimeOffset.UtcNow,
            updated_by = actorUserId
        };

        return await BaseResolvers.Update<MembershipModel, MembershipResponse, MembershipUpdateRequest, IMembershipService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IMembershipService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var validFromText = form["valid_from"].ToString();
        var validToText = form["valid_to"].ToString();

        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actorUserId = actorProvider.TryGetActorUserId(httpContext);
        if (actorUserId is null)
            return Results.Unauthorized();

        var existing = await service.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return Results.NotFound();

        var authorization = await BaseResolvers.AuthorizeModelAsync(
            httpContext,
            existing,
            RbacOperations.Update,
            cancellationToken);

        if (authorization is not null)
            return authorization;

        if (!DateTimeOffset.TryParse(validFromText, out var parsedValidFrom))
            return Results.BadRequest(new { error = "Invalid valid_from value." });

        DateTimeOffset? parsedValidTo = null;
        if (!string.IsNullOrWhiteSpace(validToText))
        {
            if (!DateTimeOffset.TryParse(validToText, out var parsed))
                return Results.BadRequest(new { error = "Invalid valid_to value." });

            parsedValidTo = parsed;
        }

        var request = new MembershipUpdateRequest(
            id,
            parsedValidFrom,
            parsedValidTo,
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
    }

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<MembershipModel, MembershipResponse, IMembershipService>(id, httpContext, service, cancellationToken);
}

public static class MembershipEndpointExtensions
{
    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/MembershipResponse").WithTags("Membership");

        group.MapPost("/create", MembershipResolvers.Create)
            .WithName("MembershipCreate")
            .ApplyJsonModel<MembershipResponse>();

        group.MapGet("/create", MembershipResolvers.CreatePage)
            .WithName("MembershipCreatePage");

        group.MapPost("/create-form", MembershipResolvers.CreateForm)
            .WithName("MembershipCreateForm");

        group.MapGet("/view/{id:guid}", MembershipResolvers.Read)
            .WithName("MembershipRead")
            .ApplyViewModel<MembershipViewPage, MembershipResponse>();

        group.MapGet("/edit/{id:guid}", MembershipResolvers.Read)
            .WithName("MembershipEdit")
            .ApplyViewModel<MembershipEditPage, MembershipResponse>();

        group.MapGet("/list", MembershipResolvers.List)
            .WithName("MembershipList")
            .ApplyViewModel<MembershipViewListPage, MembershipResponse[]>();

        group.MapPut("/update", MembershipResolvers.Update)
            .WithName("MembershipUpdate")
            .ApplyJsonModel<MembershipResponse>();

        group.MapPost("/update/{id:guid}", MembershipResolvers.UpdateForm)
            .WithName("MembershipUpdateForm")
            .ApplyViewModel<MembershipEditPage, MembershipResponse>();

        group.MapDelete("/delete", MembershipResolvers.Delete)
            .WithName("MembershipDelete");

        return endpoints;
    }
}
