using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;

using WebAppArchitecture.EntityName.Models;
using WebAppArchitecture.EntityName.Services;
using WebAppArchitecture.EntityName.UI.Pages;

namespace WebAppArchitecture.EntityName.Resolvers;

public static class EntityNameResolvers
{
    public static async Task<IResult> Create(
        EntityNameCreateRequest request,
        IEntityNameService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.Create<EntityNameModel, EntityNameResponse, EntityNameCreateRequest, IEntityNameService>(
            request,
            service,
            cancellationToken);
    }
    public static async Task<IResult> Read(
        Guid id,
        IEntityNameService service,
        CancellationToken cancellationToken)
    {
        // var model = await service.GetByIdAsync(id, cancellationToken);
        // if (model is null) return Results.NotFound();

        // return ApiResults.Ok(model);

        return await BaseResolvers.Read<EntityNameModel, EntityNameResponse, IEntityNameService>(
            id,
            service,
            cancellationToken);
    }

    public static async Task<IResult> List(
        IEntityNameService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.List<EntityNameModel, EntityNameResponse, IEntityNameService>(
            service,
            cancellationToken);
    }

    /// <summary>
    /// Accepts a Hasura-like dictionary, translates it to a typed predicate
    /// and delegates the actual filtering to the service/store layer.
    /// </summary>
    public static async Task<IResult> Vector(
        EntityNameVectorRequest request,
        IEntityNameService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var predicate = HasuraFilterExpression.Build<EntityNameModel>(request.Where);
            var models = await service.VectorAsync(predicate, cancellationToken);

            return ApiResults.Ok(models.ToArray());
        }
        catch (HasuraFilterException ex)
        {
            return Results.BadRequest(new
            {
                error = "Invalid filter.",
                detail = ex.Message
            });
        }
    }

    public static async Task<IResult> Update(
        EntityNameUpdateRequest request,
        IEntityNameService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.Update<EntityNameModel, EntityNameResponse, EntityNameUpdateRequest, IEntityNameService>(
            request,
            service,
            cancellationToken);
    }

    public static async Task<IResult> Delete(
        Guid id,
        IEntityNameService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.Delete<EntityNameModel, EntityNameResponse, IEntityNameService>(
            id,
            service,
            cancellationToken);
    }
}

public sealed record EntityNameVectorRequest(
    Dictionary<string, object?>? Where);

public static class EntityNameEndpointExtensions
{
    public static IEndpointRouteBuilder MapEntityNameEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix + "/EntityNameResponse")
            .WithTags("EntityName");

        group.MapPost("/create", EntityNameResolvers.Create)
            .WithName("EntityNameCreate")
            .ApplyJsonModel<EntityNameResponse>();

        group.MapGet("/view/{id:guid}", EntityNameResolvers.Read)
            .WithName("EntityNameRead")
            .ApplyViewModel<EntityNameViewPage, EntityNameResponse>();

        group.MapGet("/edit/{id:guid}", EntityNameResolvers.Read)
            .WithName("EntityNameEdit")
            .ApplyViewModel<EntityNameEditPage, EntityNameResponse>();

        group.MapGet("/list", EntityNameResolvers.List)
            .WithName("EntityNameList")
            .ApplyViewModel<EntityNameViewListPage, EntityNameResponse[]>();

        // Structured Hasura-like filters are sent as JSON in the request body.
        group.MapPost("/vector", EntityNameResolvers.Vector)
            .WithName("EntityNameVector")
            .ApplyJsonModel<EntityNameResponse[]>();

        group.MapPut("/update", EntityNameResolvers.Update)
            .WithName("EntityNameUpdate")
            .ApplyJsonModel<EntityNameResponse>();

        group.MapDelete("/delete", EntityNameResolvers.Delete)
            .WithName("EntityNameDelete");

        return endpoints;
    }
}
