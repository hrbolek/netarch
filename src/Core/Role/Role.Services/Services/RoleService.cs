using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Services;

public sealed class RoleService : BaseService<RoleModel, RoleResponse>, IRoleService
{
    private readonly ISecurityProjectionService _projection;

    public RoleService(ISecurityLayer security, ISecurityProjectionService projection)
        : base(security) => _projection = projection;

    public async Task<RoleResponse> CreateContainedAsync(
        RoleCreateContainedRequest request,
        CancellationToken cancellationToken = default)
    {
        var (group, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, request.group_id, "contained", cancellationToken);
        _ = await Security.GetByIdAsync<UserModel>(request.user_id, SecurityOperations.Read, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");
        _ = await Security.GetByIdAsync<RoleTypeModel>(request.role_type_id, SecurityOperations.Read, cancellationToken)
            ?? throw new KeyNotFoundException("Role type not found.");

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new RoleModel(
            Guid.NewGuid(), group.id, request.user_id, request.role_type_id,
            request.is_deputy, request.valid_from, request.valid_to, request.delegated_by_role_id,
            group.rbacobject_id, now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageCreateAsync(model, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeRoleImpactAsync([entity.group_id], ct);
            return RoleResponse.FromModel(entity);
        }, cancellationToken);
    }

    public override async Task<RoleResponse> UpdateAsync<TUpdateRequest>(TUpdateRequest model, CancellationToken cancellationToken = default)
    {
        var current = await Security.GetByIdAsync<RoleModel>(model.id, SecurityOperations.Update, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id '{model.id}' does not exist or is not accessible.");
        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageUpdateAsync(model.ApplyTo(current), ct);
            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeRoleImpactAsync([current.group_id, entity.group_id], ct);
            return RoleResponse.FromModel(entity);
        }, cancellationToken);
    }

    public override async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await Security.GetByIdAsync<RoleModel>(id, SecurityOperations.Delete, cancellationToken);
        if (current is null)
            return;

        await Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<RoleModel>(id, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeRoleImpactAsync([current.group_id], ct);
        }, cancellationToken);
    }
    public override Task<IReadOnlyCollection<RoleResponse>> VectorAsync(
        System.Linq.Expressions.Expression<Func<RoleModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDetailedQuery(predicate, operation, options, probeForMore: false);
        return Task.FromResult<IReadOnlyCollection<RoleResponse>>(query.ToArray());
    }

    public override Task<VectorPageResult<RoleResponse>> VectorPageAsync(
        System.Linq.Expressions.Expression<Func<RoleModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        var take = options.Take ?? 25;
        var skip = options.Skip ?? 0;
        var values = BuildDetailedQuery(predicate, operation, options with { Take = take }, probeForMore: true).ToArray();
        var hasMore = values.Length > take;
        IReadOnlyCollection<RoleResponse> items = hasMore ? values.Take(take).ToArray() : values;
        return Task.FromResult(new VectorPageResult<RoleResponse>(items, new VectorPageInfo(take, skip, hasMore)));
    }

    private IQueryable<RoleResponse> BuildDetailedQuery(
        System.Linq.Expressions.Expression<Func<RoleModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        bool probeForMore)
    {
        var users = Security.Query<UserModel>(SecurityOperations.Read);
        var groups = Security.Query<GroupModel>(SecurityOperations.Read);
        var roleTypes = Security.Query<RoleTypeModel>(SecurityOperations.Read);

        IQueryable<RoleModel> roles = Security.Query<RoleModel>(operation)
            .Where(predicate)
            .Where(role => users.Any(user => user.id == role.user_id))
            .Where(role => groups.Any(group => group.id == role.group_id))
            .Where(role => roleTypes.Any(roleType => roleType.id == role.role_type_id));

        roles = ApplyOrdering(roles, options.OrderBy);
        if (options.Skip is > 0)
            roles = roles.Skip(options.Skip.Value);
        if (options.Take is > 0)
            roles = roles.Take(options.Take.Value + (probeForMore ? 1 : 0));

        return
            from role in roles
            join user in users on role.user_id equals user.id
            join grp in groups on role.group_id equals grp.id
            join roleType in roleTypes on role.role_type_id equals roleType.id
            let userLabel = user.Name != ""
                ? user.Name + (user.surname != null && user.surname != "" ? " " + user.surname : "")
                : (user.email ?? "User")
            select new RoleResponse(
                role.id,
                role.group_id,
                role.user_id,
                role.role_type_id,
                roleType.name,
                role.is_deputy,
                role.valid_from,
                role.valid_to,
                role.delegated_by_role_id,
                role.rbacobject_id,
                role.created_at,
                role.updated_at,
                role.created_by,
                role.updated_by,
                grp.Name,
                userLabel,
                null,
                userLabel + " @ " + grp.Name + " [" + roleType.name + "]");
    }

}
