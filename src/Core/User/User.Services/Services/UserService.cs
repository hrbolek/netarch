using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Services;

/// <summary>
/// Služba User Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class UserService : SecuredBaseService<UserModel>, IUserService
{
    private readonly ISecurityProjectionService _projection;

    public UserService(ISecurityLayer security, IDBLayer unsecuredDb, ISecurityProjectionService projection) : base(security, unsecuredDb)
        => _projection = projection;

    public async Task<UserResponse> CreateContainedAsync(
        UserCreateContainedRequest request,
        CancellationToken cancellationToken = default)
    {
        var (group, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, request.group_id, "contained", cancellationToken);

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var validFrom = request.membership_valid_from ?? now;

        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var user = await Security.StageCreateAsync(new UserModel(
                id, request.Name, request.email, request.surname, id,
                now, now, actor, actor), ct);

            await Security.StageCreateAsync(new MembershipModel(
                Guid.NewGuid(), group.id, id, validFrom, request.membership_valid_to,
                group.rbacobject_id, now, now, actor, actor), ct);

            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeRbacObjectAsync(user.id, ct);
            return UserResponse.FromModel(user);
        }, cancellationToken);
    }
}
