using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Datová komponenta Ef Group Type Store. Připravuje dotazy a zápisy pro vyšší vrstvy bez vykreslování HTML a bez HTTP odpovědností.
/// </summary>
public sealed class EfGroupTypeStore(CoreDbContext db) : EfStore<GroupTypeModel>(db), IGroupTypeStore
{
    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách.
    /// </summary>
    public override async Task<GroupTypeModel> CreateAsync(GroupTypeModel model, CancellationToken cancellationToken = default)
    {
        var path = await BuildPathAsync(model.id, model.parent_group_type_id, cancellationToken);
        return await base.CreateAsync(model with { path = path }, cancellationToken);
    }

    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI.
    /// </summary>
    public override async Task<GroupTypeModel> UpdateAsync(GroupTypeModel model, CancellationToken cancellationToken = default)
    {
        await EnsureNoCycleAsync(model.id, model.parent_group_type_id, cancellationToken);
        var old = await Db.GroupTypes.AsNoTracking().SingleAsync(x => x.id == model.id, cancellationToken);
        var newPath = await BuildPathAsync(model.id, model.parent_group_type_id, cancellationToken);
        await RewriteDescendantPathsAsync(old.path, newPath, model.id, cancellationToken);
        return await base.UpdateAsync(model with { path = newPath }, cancellationToken);
    }

    private async Task<string> BuildPathAsync(Guid id, Guid? parentId, CancellationToken ct)
    {
        if (parentId is null) return $"/{id:D}/";
        var parent = await Db.GroupTypes.AsNoTracking().SingleAsync(x => x.id == parentId, ct);
        return $"{parent.path}{id:D}/";
    }

    private async Task EnsureNoCycleAsync(Guid id, Guid? parentId, CancellationToken ct)
    {
        if (parentId is null) return;
        var parent = await Db.GroupTypes.AsNoTracking().SingleAsync(x => x.id == parentId, ct);
        if (parent.id == id || parent.path.Contains($"/{id:D}/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Group type hierarchy cannot contain a cycle.");
    }

    private async Task RewriteDescendantPathsAsync(string oldPrefix, string newPrefix, Guid selfId, CancellationToken ct)
    {
        var descendants = await Db.GroupTypes.Where(x => x.id != selfId && x.path.StartsWith(oldPrefix)).ToArrayAsync(ct);
        foreach (var item in descendants)
            Db.GroupTypes.Update(item with { path = newPrefix + item.path[oldPrefix.Length..] });
    }
}
