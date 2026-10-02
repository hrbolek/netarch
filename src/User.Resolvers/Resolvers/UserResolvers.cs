using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;

using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Services;
using WebAppArchitecture.User.UI.Pages;

namespace WebAppArchitecture.User.Resolvers;

public static class UserResolvers
{
    public static async Task<IResult> Create(
        UserCreateRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.Create<UserModel, UserResponse, UserCreateRequest, IUserService>(
            request,
            service,
            cancellationToken);
    }
    public static async Task<IResult> Read(
        Guid id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        // var model = await service.GetByIdAsync(id, cancellationToken);
        // if (model is null) return Results.NotFound();

        // return ApiResults.Ok(model);

        return await BaseResolvers.Read<UserModel, UserResponse, IUserService>(
            id,
            service,
            cancellationToken);
    }

    public static async Task<IResult> List(
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.List<UserModel, UserResponse, IUserService>(
            service,
            cancellationToken);
    }

    /// <summary>
    /// Accepts a Hasura-like dictionary, translates it to a typed predicate
    /// and delegates the actual filtering to the service/store layer.
    /// </summary>
    public static async Task<IResult> Vector(
        UserVectorRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var predicate = HasuraFilterExpression.Build<UserModel>(request.Where);
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
        UserUpdateRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.Update<UserModel, UserResponse, UserUpdateRequest, IUserService>(
            request,
            service,
            cancellationToken);
    }

    public static async Task<IResult> Delete(
        Guid id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.Delete<UserModel, UserResponse, IUserService>(
            id,
            service,
            cancellationToken);
    }
}

public sealed record UserVectorRequest(
    Dictionary<string, object?>? Where);

public static class UserEndpointExtensions
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix + "/UserResponse")
            .WithTags("User");

        group.MapPost("/create", UserResolvers.Create)
            .WithName("UserCreate")
            .ApplyJsonModel<UserResponse>();

        group.MapGet("/view/{id:guid}", UserResolvers.Read)
            .WithName("UserRead")
            .ApplyViewModel<UserViewPage, UserResponse>();

        group.MapGet("/edit/{id:guid}", UserResolvers.Read)
            .WithName("UserEdit")
            .ApplyViewModel<UserEditPage, UserResponse>();

        group.MapGet("/list", UserResolvers.List)
            .WithName("UserList")
            .ApplyViewModel<UserViewListPage, UserResponse[]>();

        // Structured Hasura-like filters are sent as JSON in the request body.
        group.MapPost("/vector", UserResolvers.Vector)
            .WithName("UserVector")
            .ApplyJsonModel<UserResponse[]>();

        group.MapPut("/update", UserResolvers.Update)
            .WithName("UserUpdate")
            .ApplyJsonModel<UserResponse>();

        group.MapDelete("/delete", UserResolvers.Delete)
            .WithName("UserDelete");

        return endpoints;
    }
}
