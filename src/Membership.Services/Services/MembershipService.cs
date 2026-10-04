using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Services;

public interface IMembershipService : IBaseService<MembershipModel, MembershipResponse>
{
}

public sealed class MembershipService : BaseService<MembershipModel, MembershipResponse>, IMembershipService
{
    private readonly ISecurityProjectionService _projection;

    public MembershipService(ISecurityLayer security, ISecurityProjectionService projection)
        : base(security) => _projection = projection;

    public override Task<MembershipResponse> CreateAsync<TCreateRequest>(
        TCreateRequest model,
        CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageCreateAsync(model.ToModel(), ct);
            await Security.SaveChangesAsync(ct);
            await _projection.RebuildAllAsync(ct);
            return MembershipResponse.FromModel(entity);
        }, cancellationToken);

    public override async Task<MembershipResponse> UpdateAsync<TUpdateRequest>(
        TUpdateRequest model,
        CancellationToken cancellationToken = default)
    {
        var current = await Security.GetByIdAsync<MembershipModel>(
            model.id,
            SecurityOperations.Update,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Item with id '{model.id}' does not exist or is not accessible.");

        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageUpdateAsync(model.ApplyTo(current), ct);
            await Security.SaveChangesAsync(ct);
            await _projection.RebuildAllAsync(ct);
            return MembershipResponse.FromModel(entity);
        }, cancellationToken);
    }

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<MembershipModel>(id, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.RebuildAllAsync(ct);
        }, cancellationToken);
}
