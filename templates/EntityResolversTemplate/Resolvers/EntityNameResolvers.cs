using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;
using RootNamespace.EntityName.Models;
using RootNamespace.EntityName.Services;
using RootNamespace.EntityName.UI.Pages;

namespace RootNamespace.EntityName.Resolvers;

public static class EntityNameResolvers
{
    public static async Task<IResult> Read(
        Guid id,
        IEntityNameService service,
        CancellationToken cancellationToken)
    {
        var model = await service.GetByIdAsync(id, cancellationToken);
        if (model is null) return Results.NotFound();

        return ApiResults.Ok(new EntityNameResponse(model.id, model.Name));
    }

    public static async Task<IResult> List(
        IEntityNameService service,
        CancellationToken cancellationToken)
    {
        var models = await service.GetAllAsync(cancellationToken);
        return ApiResults.Ok(models.Select(ToResponse).ToArray());
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

            return ApiResults.Ok(models.Select(ToResponse).ToArray());
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

    private static EntityNameResponse ToResponse(EntityNameModel model)
        => new(model.id, model.Name);
}

public sealed record EntityNameVectorRequest(
    Dictionary<string, object?>? Where);

public static class EntityNameEndpointExtensions
{
    public static IEndpointRouteBuilder MapEntityNameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/entity-route")
            .WithTags("EntityName");

        group.MapGet("/{id:guid}", EntityNameResolvers.Read)
            .WithName("EntityNameRead")
            .ApplyViewModel<EntityNameViewPage, EntityNameResponse>();

        group.MapGet("/{id:guid}/edit", EntityNameResolvers.Read)
            .WithName("EntityNameEdit")
            .ApplyViewModel<EntityNameEditPage, EntityNameResponse>();

        group.MapGet("/list", EntityNameResolvers.List)
            .WithName("EntityNameList")
            .ApplyJsonModel<EntityNameResponse[]>();

        // Structured Hasura-like filters are sent as JSON in the request body.
        group.MapPost("/vector", EntityNameResolvers.Vector)
            .WithName("EntityNameVector")
            .ApplyJsonModel<EntityNameResponse[]>();

        return endpoints;
    }
}
