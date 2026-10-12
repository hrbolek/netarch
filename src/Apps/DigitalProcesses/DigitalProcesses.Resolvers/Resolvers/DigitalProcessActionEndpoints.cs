using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

/// <summary>
/// Common contextual actions for DigitalProcesses entities.
/// Never expose an action for a record which is not readable through RBAC.
/// Entity-specific actions (publish, clone, transitions) remain on their owning editors.
/// </summary>
/// <summary>
/// Typ Digital Process Action Endpoints ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public static class DigitalProcessActionEndpoints
{
    public static RouteHandlerBuilder MapActions<TEntity>(
        this RouteGroupBuilder group, string entityName)
        where TEntity : class, ISecuredEntityDBModel
    {
        return group.MapGet("/actions/{id:guid}", (Guid id, ISecurityLayer security, CancellationToken ct) =>
                BuildActionsAsync<TEntity>(id, entityName, security, ct))
            .ApplyViewModel<EntityActionMenu, EntityActionMenuModel>()
            .WithDescription($"Returns the contextual, RBAC-filtered action menu for {entityName}. " +
                "Use Accept: application/json or text/html to select the representation.");
    }

    private static async Task<object> BuildActionsAsync<TEntity>(
        Guid id, string entityName, ISecurityLayer security, CancellationToken ct)
        where TEntity : class, ISecuredEntityDBModel
    {
        var entity = await security.GetByIdAsync<TEntity>(id, SecurityOperations.Read, ct);
        if (entity is null) return Results.NotFound();

        var root = $"/digital-processes/{entityName}";
        var actions = new EntityAction[]
        {
            new("view", "View", $"{root}/view/{id}",
                EntityActionKind.Navigate, Group: "navigation", Order: 10)
        };
        return new EntityActionMenuModel(actions);
    }
}
