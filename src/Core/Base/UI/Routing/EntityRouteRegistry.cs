namespace WebAppArchitecture.Base.UI.Routing;

public sealed record EntityRouteDefinition(
    string App,
    string Entity,
    string Operation = "view");

public interface IEntityRouteRegistry
{
    EntityRouteDefinition Resolve(
        Type dtoType,
        string? app = null,
        string? entity = null,
        string? operation = null);

    string GetUrl(
        Type dtoType,
        Guid id,
        string? app = null,
        string? entity = null,
        string? operation = null);

    void Register<TDTO>(
        string? app = null,
        string? entity = null,
        string? operation = null);
}

/// <summary>
/// Central registry for UI routes of entity DTOs.
/// Convention is /{app}/{entity}/{operation}/{id} with defaults:
/// app=architecture, entity=DTO type name, operation=view.
///
/// Resolution order for each route element is:
/// local Razor override -> registered entity/type override -> convention.
/// </summary>
public sealed class EntityRouteRegistry : IEntityRouteRegistry
{
    private readonly Dictionary<Type, EntityRouteDefinition> byType = new();
    private readonly Dictionary<string, EntityRouteDefinition> byEntity =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object gate = new();

    public EntityRouteDefinition Resolve(
        Type dtoType,
        string? app = null,
        string? entity = null,
        string? operation = null)
    {
        lock (gate)
        {
            var conventional = new EntityRouteDefinition("architecture", dtoType.Name, "view");

            EntityRouteDefinition registered;
            if (!string.IsNullOrWhiteSpace(entity) && byEntity.TryGetValue(entity!, out var entityDefinition))
            {
                registered = entityDefinition;
            }
            else if (byType.TryGetValue(dtoType, out var typeDefinition))
            {
                registered = typeDefinition;
            }
            else
            {
                registered = conventional;
            }

            return registered with
            {
                App = string.IsNullOrWhiteSpace(app) ? registered.App : app!,
                Entity = string.IsNullOrWhiteSpace(entity) ? registered.Entity : entity!,
                Operation = string.IsNullOrWhiteSpace(operation) ? registered.Operation : operation!
            };
        }
    }

    public string GetUrl(
        Type dtoType,
        Guid id,
        string? app = null,
        string? entity = null,
        string? operation = null)
    {
        var route = Resolve(dtoType, app, entity, operation);
        return $"/{route.App}/{route.Entity}/{route.Operation}/{id}";
    }

    public void Register<TDTO>(
        string? app = null,
        string? entity = null,
        string? operation = null)
    {
        var type = typeof(TDTO);
        var definition = new EntityRouteDefinition(
            string.IsNullOrWhiteSpace(app) ? "architecture" : app!,
            string.IsNullOrWhiteSpace(entity) ? type.Name : entity!,
            string.IsNullOrWhiteSpace(operation) ? "view" : operation!);

        lock (gate)
        {
            byType[type] = definition;
            byEntity[definition.Entity] = definition;
        }
    }
}
