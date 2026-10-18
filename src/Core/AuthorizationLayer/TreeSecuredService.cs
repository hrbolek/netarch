using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>
/// Secure-by-default base for self-referencing tree entities (events, event types, ...).
/// Structure is expressed only through the materialized path, so children, subtrees and
/// ancestors are plain prefix queries over RBAC-filtered sources. Reads never leave the
/// secured source; path maintenance on writes (including rewriting descendants when a node
/// moves) is a structural invariant and is therefore the only unsecured write done here.
/// </summary>

public abstract class TreeSecuredService<TDBModel> : SecuredEntityService<TDBModel>
    where TDBModel : class, ITreeEntityDBModel<TDBModel>
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    protected TreeSecuredService(IAuthorizationLayer security, IDBLayer db, ISecurityQueryProvider? provider = null)
        : base(security, db, provider) { }

    /// <summary>Default sibling ordering; override with a domain ordering.</summary>
    protected virtual IQueryable<TDBModel> Order(IQueryable<TDBModel> query) => query.OrderBy(x => x.path);

    protected IQueryable<TDBModel> Page(IQueryable<TDBModel> query, int? skip, int? take)
        => Order(query).Skip(Math.Max(0, skip ?? 0)).Take(Math.Clamp(take ?? DefaultPageSize, 1, MaxPageSize));

    /// <summary>Authorized path of a node, or null when the node does not exist or is not accessible.</summary>
    protected Task<string?> PathOfAsync(Guid id, string operation, CancellationToken ct)
        => SecuredModels(operation).Where(x => x.id == id).Select(x => x.path).FirstOrDefaultAsync(ct);

    /// <summary>Direct children of the node with the given path, authorized for the operation.</summary>
    protected IQueryable<TDBModel> SecuredChildren(string parentPath, string operation)
    {
        var childLength = parentPath.Length + TreePath.SegmentLength;
        return SecuredModels(operation).Where(x => x.path.StartsWith(parentPath) && x.path.Length == childLength);
    }

    /// <summary>All descendants (excluding the node itself) of the node with the given path.</summary>
    protected IQueryable<TDBModel> SecuredSubtree(string rootPath, string operation)
        => SecuredModels(operation).Where(x => x.path.StartsWith(rootPath) && x.path != rootPath);

    /// <summary>Authorized direct children as a page; empty when the parent is not accessible.</summary>
    public async Task<List<TDBModel>> ChildrenAsync(Guid parentId, int? skip, int? take, CancellationToken ct)
    {
        var path = await PathOfAsync(parentId, SecurityOperations.Read, ct);
        return path is null ? new() : await Page(SecuredChildren(path, SecurityOperations.Read), skip, take).ToListAsync(ct);
    }

    /// <summary>Authorized descendants of a root, optionally filtered in SQL; bounded by <paramref name="limit"/>.</summary>
    public async Task<List<TDBModel>> DescendantsAsync(Guid rootId, Expression<Func<TDBModel, bool>>? filter, int limit, CancellationToken ct)
    {
        var path = await PathOfAsync(rootId, SecurityOperations.Read, ct);
        if (path is null) return new();
        var query = SecuredSubtree(path, SecurityOperations.Read);
        if (filter is not null) query = query.Where(filter);
        return await Order(query).Take(Math.Max(1, limit)).ToListAsync(ct);
    }

    /// <summary>Authorized ancestors in root-to-parent order; ancestors the actor cannot read are omitted.</summary>
    public async Task<List<TDBModel>> AncestorsAsync(Guid id, CancellationToken ct)
    {
        var path = await PathOfAsync(id, SecurityOperations.Read, ct);
        if (path is null) return new();
        var ids = TreePath.AncestorIds(path).ToArray();
        if (ids.Length == 0) return new();
        var found = await SecuredModels(SecurityOperations.Read).Where(x => ids.Contains(x.id)).ToListAsync(ct);
        return found.OrderBy(x => x.path.Length).ToList();
    }

    /// <summary>True when moving <paramref name="id"/> under <paramref name="parentId"/> would create a cycle.</summary>
    public async Task<bool> CreatesCycleAsync(Guid id, Guid? parentId, CancellationToken ct)
    {
        if (parentId is not Guid pid) return false;
        if (pid == id) return true;
        var parentPath = await Unsecured_Query().Where(x => x.id == pid).Select(x => x.path).FirstOrDefaultAsync(ct);
        return parentPath is not null && TreePath.ContainsNode(parentPath, id);
    }

    /// <summary>True when the node has any descendant (privileged invariant check).</summary>
    protected async Task<bool> HasDescendantsAsync(Guid id, CancellationToken ct)
    {
        var path = await Unsecured_Query().Where(x => x.id == id).Select(x => x.path).FirstOrDefaultAsync(ct);
        return path is not null && await Unsecured_Query().AnyAsync(x => x.path.StartsWith(path) && x.id != id, ct);
    }

    /// <summary>Stage a new node: derives its path from the parent and authorizes through the security layer.</summary>
    protected async Task<TDBModel> StageCreateNodeAsync(TDBModel model, CancellationToken ct)
    {
        var path = TreePath.Root(model.id);
        if (model.parent_id is Guid pid)
        {
            var parentPath = await Unsecured_Query().Where(x => x.id == pid).Select(x => x.path).FirstOrDefaultAsync(ct);
            if (string.IsNullOrEmpty(parentPath))
                throw new InvalidOperationException($"Parent {pid:D} does not exist or has no materialized path.");
            path = TreePath.Child(parentPath, model.id);
        }
        return await Security.StageCreateAsync(model.WithPath(path), ct);
    }

    /// <summary>
    /// Stage an update of a node, recomputing its path and rewriting the paths of its subtree when the parent changed.
    /// The node itself is authorized by the security layer; descendants only follow the move structurally.
    /// </summary>
    protected async Task<TDBModel> StageUpdateNodeAsync(TDBModel model, CancellationToken ct)
    {
        var oldPath = await Unsecured_Query().Where(x => x.id == model.id).Select(x => x.path).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Cannot update a missing tree node.");
        var newPath = TreePath.Root(model.id);
        if (model.parent_id is Guid pid)
        {
            if (await CreatesCycleAsync(model.id, pid, ct)) throw new InvalidOperationException("Hierarchy cannot contain a cycle.");
            var parentPath = await Unsecured_Query().Where(x => x.id == pid).Select(x => x.path).FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException($"Parent {pid:D} does not exist.");
            newPath = TreePath.Child(parentPath, model.id);
        }
        var updated = await Security.StageUpdateAsync(model.WithPath(newPath), ct);
        if (!string.Equals(oldPath, newPath, StringComparison.Ordinal))
        {
            var descendants = await Unsecured_Query().Where(x => x.path.StartsWith(oldPath) && x.id != model.id).ToListAsync(ct);
            foreach (var item in descendants)
                await Unsecured_StageUpdateAsync(item.WithPath(newPath + item.path[oldPath.Length..]), ct);
        }
        return updated;
    }

    /// <summary>Delete a node the actor may delete, only when it has no descendants.</summary>
    protected async Task<bool> TryDeleteLeafAsync(Guid id, CancellationToken ct)
    {
        if (await SecuredModels(SecurityOperations.Delete).AnyAsync(x => x.id == id, ct) is false) return false;
        if (await HasDescendantsAsync(id, ct)) return false;
        await DeleteAsync(id, ct);
        return true;
    }
}
