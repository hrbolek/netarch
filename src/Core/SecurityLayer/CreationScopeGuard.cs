using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Trusted authorization target used before a new secured entity exists.
/// The target is always derived server-side from a parent, type or well-known catalog scope.
/// </summary>
public static class CreationScopeGuard
{
    public static Task<CreationScope> RequireCatalogAsync(
        ISecurityLayer security,
        Guid scopeId,
        CancellationToken cancellationToken = default)
        => RequireRbacAsync(security, scopeId, "typed", scopeId, cancellationToken);

    public static Task<CreationScope> RequireRbacAsync(
        ISecurityLayer security,
        Guid rbacObjectId,
        string kind,
        Guid? sourceId = null,
        CancellationToken cancellationToken = default)
        => RequireRbacCoreAsync(security, rbacObjectId, kind, sourceId, cancellationToken);

    public static Task<(TEntity Entity, CreationScope Scope)> RequireEntityAsync<TEntity>(
        ISecurityLayer security,
        Guid id,
        string kind = "contained",
        CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
    {
        var entity = security.Query<TEntity>(SecurityOperations.Create).SingleOrDefault(x => x.id == id)
            ?? throw new UnauthorizedAccessException(
                $"Create is not allowed in {typeof(TEntity).Name} '{id}', or the object does not exist.");

        return Task.FromResult((entity, new CreationScope(entity.rbacobject_id, kind, entity.id)));
    }

    public static async Task<GroupModel> StageOwnedGroupAsync(
        ISecurityLayer security,
        Guid id,
        string name,
        GroupModel parent,
        bool inheritRoles,
        DateTimeOffset now,
        Guid actor,
        CancellationToken cancellationToken = default)
    {
        var path = $"{parent.path}{id:D}/";
        return await security.StageCreateAsync(new GroupModel(
            id, name, parent.group_type_id, parent.id, path, inheritRoles, id,
            now, now, actor, actor), cancellationToken);
    }

    private static async Task<CreationScope> RequireRbacCoreAsync(
        ISecurityLayer security,
        Guid rbacObjectId,
        string kind,
        Guid? sourceId,
        CancellationToken cancellationToken)
    {
        if (!await security.IsAllowedAsync(rbacObjectId, SecurityOperations.Create, cancellationToken))
            throw new UnauthorizedAccessException($"Create is not allowed in RBAC scope '{rbacObjectId}'.");

        return new CreationScope(rbacObjectId, kind, sourceId);
    }
}
