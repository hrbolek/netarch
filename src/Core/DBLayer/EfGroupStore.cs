using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Datová komponenta Ef Group Store. Připravuje dotazy a zápisy pro vyšší vrstvy bez vykreslování HTML a bez HTTP odpovědností.
/// </summary>
public sealed class EfGroupStore(ApplicationDbContext db) : EfStore<GroupModel>(db), IGroupStore
{
    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách.
    /// </summary>
    public override async Task<GroupModel> CreateAsync(GroupModel model, CancellationToken cancellationToken = default)
    {
        var path = await BuildPathAsync(model.id, model.master_group_id, cancellationToken);
        var normalized = model with { path = path, rbacobject_id = model.id };
        return await base.CreateAsync(normalized, cancellationToken);
    }

    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI.
    /// </summary>
    public override async Task<GroupModel> UpdateAsync(GroupModel model, CancellationToken cancellationToken = default)
    {
        await EnsureNoCycleAsync(model.id, model.master_group_id, cancellationToken);
        var old = await Db.Groups.AsNoTracking().SingleAsync(x => x.id == model.id, cancellationToken);
        var newPath = await BuildPathAsync(model.id, model.master_group_id, cancellationToken);
        await RewriteDescendantPathsAsync(old.path, newPath, model.id, cancellationToken);
        return await base.UpdateAsync(model with { path = newPath, rbacobject_id = model.id }, cancellationToken);
    }

    private async Task<string> BuildPathAsync(Guid id, Guid? parentId, CancellationToken ct)
    {
        if (parentId is null) return $"/{id:D}/";
        // A process request can create multiple nested RBAC groups within one
        // transaction. The parent might still be tracked as Added and therefore
        // is not visible to an EF query against the database yet.
        var parent = Db.Groups.Local.FirstOrDefault(x => x.id == parentId.Value)
            ?? await Db.Groups.AsNoTracking().SingleOrDefaultAsync(x => x.id == parentId.Value, ct)
            ?? throw new InvalidOperationException($"Parent group {parentId:D} does not exist.");
        if (string.IsNullOrWhiteSpace(parent.path))
            throw new InvalidOperationException($"Parent group {parentId:D} has no materialized path.");
        return $"{parent.path}{id:D}/";
    }

    private async Task EnsureNoCycleAsync(Guid id, Guid? parentId, CancellationToken ct)
    {
        if (parentId is null) return;
        var parent = await Db.Groups.AsNoTracking().SingleAsync(x => x.id == parentId, ct);
        if (parent.id == id || parent.path.Contains($"/{id:D}/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Group hierarchy cannot contain a cycle.");
    }

    private async Task RewriteDescendantPathsAsync(string oldPrefix, string newPrefix, Guid selfId, CancellationToken ct)
    {
        var descendants = await Db.Groups.Where(x => x.id != selfId && x.path.StartsWith(oldPrefix)).ToArrayAsync(ct);
        foreach (var item in descendants)
            Db.Groups.Update(item with { path = newPrefix + item.path[oldPrefix.Length..] });
    }
}
