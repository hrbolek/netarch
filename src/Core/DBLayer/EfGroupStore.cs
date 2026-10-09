using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

public sealed class EfGroupStore(ApplicationDbContext db) : EfStore<GroupModel>(db), IGroupStore
{
    public override async Task<GroupModel> CreateAsync(GroupModel model, CancellationToken cancellationToken = default)
    {
        var path = await BuildPathAsync(model.id, model.master_group_id, cancellationToken);
        var normalized = model with { path = path, rbacobject_id = model.id };
        return await base.CreateAsync(normalized, cancellationToken);
    }

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
        var parent = await Db.Groups.AsNoTracking().SingleAsync(x => x.id == parentId, ct);
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
