using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

public abstract class EfStore<TModel> : IBaseStore<TModel> where TModel : class, IEntityDBModel
{
    protected readonly ApplicationDbContext Db;
    protected DbSet<TModel> Set => Db.Set<TModel>();
    protected IQueryable<TModel> RawQuery => Set.AsNoTracking();

    public IQueryable<TModel> Query() => RawQuery;

    protected EfStore(ApplicationDbContext db) => Db = db;

    public virtual Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => RawQuery.SingleOrDefaultAsync(x => x.id == id, cancellationToken);

    public virtual async Task<IReadOnlyCollection<TModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await RawQuery.ToArrayAsync(cancellationToken);

    public virtual async Task<IReadOnlyCollection<TModel>> VectorAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default)
        => await RawQuery.Where(predicate).ToArrayAsync(cancellationToken);

    public virtual Task<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        Set.Add(model);
        return Task.FromResult(model);
    }

    public virtual Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        Set.Update(model);
        return Task.FromResult(model);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await Set.FindAsync([id], cancellationToken);
        if (model is null) return;
        Set.Remove(model);
    }
}

public sealed class EfUserStore(ApplicationDbContext db) : EfStore<UserModel>(db), IUserStore;
public sealed class EfRoleStore(ApplicationDbContext db) : EfStore<RoleModel>(db), IRoleStore
{
    public override async Task<RoleModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Roles.AsNoTracking().Include(x => x.role_type).SingleOrDefaultAsync(x => x.id == id, cancellationToken);

    public override async Task<IReadOnlyCollection<RoleModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Db.Roles.AsNoTracking().Include(x => x.role_type).ToArrayAsync(cancellationToken);

    public override Task<RoleModel> CreateAsync(RoleModel model, CancellationToken cancellationToken = default)
    {
        Db.Roles.Add(model);
        return Task.FromResult(model);
    }

    public override Task<RoleModel> UpdateAsync(RoleModel model, CancellationToken cancellationToken = default)
    {
        Db.Roles.Update(model);
        return Task.FromResult(model);
    }
}
public sealed class EfMembershipStore(ApplicationDbContext db) : EfStore<MembershipModel>(db), IMembershipStore;
public sealed class EfRoleTypeStore(ApplicationDbContext db) : EfStore<RoleTypeModel>(db), IRoleTypeStore
{
    public override async Task<RoleTypeModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.RoleTypes.AsNoTracking()
            .Include(x => x.operation_links).ThenInclude(x => x.operation)
            .SingleOrDefaultAsync(x => x.id == id, cancellationToken);

    public override async Task<IReadOnlyCollection<RoleTypeModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Db.RoleTypes.AsNoTracking()
            .Include(x => x.operation_links).ThenInclude(x => x.operation)
            .ToArrayAsync(cancellationToken);

    public override async Task<RoleTypeModel> CreateAsync(RoleTypeModel model, CancellationToken cancellationToken = default)
    {
        var clean = model with { operation_links = new List<RoleTypeOperationModel>() };
        Db.RoleTypes.Add(clean);
        await SyncOperationsAsync(clean.id, model.requested_operations, cancellationToken);
        return clean;
    }

    public override async Task<RoleTypeModel> UpdateAsync(RoleTypeModel model, CancellationToken cancellationToken = default)
    {
        var existing = await Db.RoleTypes.SingleAsync(x => x.id == model.id, cancellationToken);
        Db.Entry(existing).CurrentValues.SetValues(model);
        await SyncOperationsAsync(model.id, model.requested_operations, cancellationToken);
        return model;
    }

    private async Task SyncOperationsAsync(Guid roleTypeId, IEnumerable<string> operationNames, CancellationToken ct)
    {
        var names = operationNames.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var operations = await Db.Operations.Where(x => names.Contains(x.name)).ToArrayAsync(ct);
        var missing = names.Except(operations.Select(x => x.name), StringComparer.OrdinalIgnoreCase).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Unknown operations: {string.Join(", ", missing)}. Register operations before assigning them to a RoleType.");

        var current = await Db.RoleTypeOperations.Where(x => x.role_type_id == roleTypeId).ToArrayAsync(ct);
        Db.RoleTypeOperations.RemoveRange(current);
        foreach (var operation in operations)
            Db.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), roleTypeId, operation.id));
    }
}

public sealed class EfGroupTypeStore(ApplicationDbContext db) : EfStore<GroupTypeModel>(db), IGroupTypeStore
{
    public override async Task<GroupTypeModel> CreateAsync(GroupTypeModel model, CancellationToken cancellationToken = default)
    {
        var path = await BuildPathAsync(model.id, model.parent_group_type_id, cancellationToken);
        return await base.CreateAsync(model with { path = path }, cancellationToken);
    }

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

public sealed class EfGroupStore(ApplicationDbContext db) : EfStore<GroupModel>(db), IGroupStore
{
    public override async Task<GroupModel> CreateAsync(GroupModel model, CancellationToken cancellationToken = default)
    {
        var path = await BuildPathAsync(model.id, model.master_group_id, cancellationToken);
        var normalized = model with { path = path, rbac_id = model.id };
        return await base.CreateAsync(normalized, cancellationToken);
    }

    public override async Task<GroupModel> UpdateAsync(GroupModel model, CancellationToken cancellationToken = default)
    {
        await EnsureNoCycleAsync(model.id, model.master_group_id, cancellationToken);
        var old = await Db.Groups.AsNoTracking().SingleAsync(x => x.id == model.id, cancellationToken);
        var newPath = await BuildPathAsync(model.id, model.master_group_id, cancellationToken);
        await RewriteDescendantPathsAsync(old.path, newPath, model.id, cancellationToken);
        return await base.UpdateAsync(model with { path = newPath, rbac_id = model.id }, cancellationToken);
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
