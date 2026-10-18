using WebAppArchitecture.Base.Services;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Services;

/// <summary>
/// Služba Membership Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public sealed class MembershipService : SecuredEntityService<MembershipModel>, IMembershipService
{
    private readonly ISecurityProjectionService _projection;

    public MembershipService(IAuthorizationLayer security, IDBLayer unsecuredDb, ISecurityProjectionService projection)
        : base(security, unsecuredDb) => _projection = projection;

    public async Task<MembershipResponse> CreateContainedAsync(
        MembershipCreateContainedRequest request,
        CancellationToken cancellationToken = default)
    {
        var (group, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, request.group_id, "contained", cancellationToken);
        _ = await Security.GetByIdAsync<UserModel>(request.user_id, SecurityOperations.Read, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new MembershipModel(
            Guid.NewGuid(), group.id, request.user_id, request.valid_from, request.valid_to,
            group.rbacobject_id, now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageCreateAsync(model, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeRbacObjectAsync(entity.user_id, ct);
            return MembershipResponse.FromModel(entity);
        }, cancellationToken);
    }

    public override async Task<TDTO> UpdateAsync<TUpdateRequest, TDTO>(TUpdateRequest model, CancellationToken cancellationToken = default)
    {
        var current = await Security.GetByIdAsync<MembershipModel>(model.id, SecurityOperations.Update, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id '{model.id}' does not exist or is not accessible.");
        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageUpdateAsync(model.ApplyTo(current), model.updated_at, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeRbacObjectsAsync(new[] { current.user_id, entity.user_id }, ct);
            return TDTO.FromModel(entity);
        }, cancellationToken);
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: GetByIdAsync, ExecuteInTransactionAsync, StageDeleteAsync, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public override async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await Security.GetByIdAsync<MembershipModel>(id, SecurityOperations.Delete, cancellationToken);
        if (current is null)
            return;

        await Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<MembershipModel>(id, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeRbacObjectAsync(current.user_id, ct);
        }, cancellationToken);
    }
    public override Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(
        System.Linq.Expressions.Expression<Func<MembershipModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        if (typeof(TDTO) != typeof(MembershipResponse))
            return base.VectorAsync<TDTO>(predicate, operation, options, cancellationToken);
        IReadOnlyCollection<TDTO> values = BuildDetailedQuery(predicate, operation, options, probeForMore: false)
            .AsEnumerable().Select(item => (TDTO)(object)item).ToArray();
        return Task.FromResult(values);
    }

    public override Task<VectorPageResult<TDTO>> VectorPageAsync<TDTO>(
        System.Linq.Expressions.Expression<Func<MembershipModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        if (typeof(TDTO) != typeof(MembershipResponse))
            return base.VectorPageAsync<TDTO>(predicate, operation, options, cancellationToken);
        var take = options.Take ?? 25;
        var skip = options.Skip ?? 0;
        var values = BuildDetailedQuery(predicate, operation, options with { Take = take }, probeForMore: true).ToArray();
        var hasMore = values.Length > take;
        IReadOnlyCollection<TDTO> items = (hasMore ? values.Take(take) : values).Select(item => (TDTO)(object)item).ToArray();
        return Task.FromResult(new VectorPageResult<TDTO>(items, new VectorPageInfo(take, skip, hasMore)));
    }

    private IQueryable<MembershipResponse> BuildDetailedQuery(
        System.Linq.Expressions.Expression<Func<MembershipModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        bool probeForMore)
    {
        var users = Security.Query<UserModel>(SecurityOperations.Read);
        var groups = Security.Query<GroupModel>(SecurityOperations.Read);

        IQueryable<MembershipModel> memberships = Security.Query<MembershipModel>(operation)
            .Where(predicate)
            .Where(membership => users.Any(user => user.id == membership.user_id))
            .Where(membership => groups.Any(group => group.id == membership.group_id));

        memberships = ApplyOrdering(memberships, options.OrderBy);
        if (options.Skip is > 0)
            memberships = memberships.Skip(options.Skip.Value);
        if (options.Take is > 0)
            memberships = memberships.Take(options.Take.Value + (probeForMore ? 1 : 0));

        return
            from membership in memberships
            join user in users on membership.user_id equals user.id
            join grp in groups on membership.group_id equals grp.id
            let userLabel = user.Name != ""
                ? user.Name + (user.surname != null && user.surname != "" ? " " + user.surname : "")
                : (user.email ?? "User")
            select new MembershipResponse(
                membership.id,
                membership.group_id,
                membership.user_id,
                membership.valid_from,
                membership.valid_to,
                membership.rbacobject_id,
                membership.created_at,
                membership.updated_at,
                membership.created_by,
                membership.updated_by,
                grp.Name,
                userLabel,
                userLabel + " @ " + grp.Name);
    }

}
