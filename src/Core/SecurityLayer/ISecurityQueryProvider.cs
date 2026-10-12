using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>SQL-composable read policy. Apply before materialization; deny by default if identity is missing.</summary>
public interface ISecurityQueryProvider
{
    IQueryable<TModel> Query<TModel>(string operation) where TModel : class, ISecuredEntityDBModel;
}

/// <summary>Adapter to the existing PostgreSQL-translatable EffectiveAccess RBAC query.</summary>
internal sealed class RbacSecurityQueryProvider(ISecurityLayer security) : ISecurityQueryProvider
{
    public IQueryable<TModel> Query<TModel>(string operation) where TModel : class, ISecuredEntityDBModel
        => security.Query<TModel>(operation);
}
