namespace WebAppArchitecture.Base.UI.Routing;

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
