using WebAppArchitecture.DigitalProcesses.Services.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Components;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
/// <summary>
/// Common contextual actions for DigitalProcesses entities.
/// Never expose an action for a record which is not readable through RBAC.
/// Entity-specific actions (publish, clone, transitions) remain on their owning editors.
/// </summary>
public static class DigitalProcessActionEndpoints
{
    public static RouteHandlerBuilder MapActions<TEntity>(
        this RouteGroupBuilder group, string entityName)
        where TEntity : class, ISecuredEntityDBModel
    {
        return group.MapGet("/actions/{id:guid}", DigitalProcessActionResolvers.Read<TEntity>)
            .ApplyViewModel<EntityActionMenu, EntityActionMenuModel>()
            .WithDescription($"Returns the contextual, RBAC-filtered action menu for {entityName}. " +
                "Use Accept: application/json or text/html to select the representation.");
    }

}

public static class DigitalProcessActionResolvers
{
    public static async Task<object> Read<TEntity>(
        Guid id, IDigitalProcessActionService<TEntity> service, CancellationToken ct)
        where TEntity : class, ISecuredEntityDBModel
    {
        if (!await service.CanReadAsync(id, ct)) return Results.NotFound();

        var entityName = typeof(TEntity).Name.Replace("Model", "Response", StringComparison.Ordinal);
        var root = $"/digital-processes/{entityName}";
        var actions = new EntityAction[]
        {
            new("view", "View", $"{root}/view/{id}",
                EntityActionKind.Navigate, Group: "navigation", Order: 10)
        };
        return new EntityActionMenuModel(actions);
    }
}
