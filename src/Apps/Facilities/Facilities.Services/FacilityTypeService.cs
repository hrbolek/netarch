using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Facilities;

public sealed class FacilityTypeService : SecuredEntityService<FacilityTypeModel>, IFacilityTypeService
{
    private readonly IDBLayer db;
    private readonly CoreDbContext context;

    public FacilityTypeService(IAuthorizationLayer security, IDBLayer db, CoreDbContext context) : base(security, db)
    {
        this.db = db;
        this.context = context;
    }

    public async Task<IReadOnlyList<FacilityTypeResponse>> ListAsync(string? search = null, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var query = SecuredModels(SecurityOperations.Read);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        return await query.OrderBy(x => x.Name).ThenBy(x => x.id).Skip(FacilitiesWriteBoundary.Skip(skip))
            .Take(FacilitiesWriteBoundary.Take(take)).Select(FacilityTypeResponse.Projection).ToListAsync(ct);
    }

    public Task<FacilityTypeResponse> CreateAsync(FacilityTypeCreateRequest request, CancellationToken ct = default)
        => FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            Validate(request.Code, request.Name, request.AllowedChildTypeIds);
            await FacilitiesWriteBoundary.RequireScopeAsync(Security, db, request.RbacObjectId, c);
            await RequireChildTypes(request.AllowedChildTypeIds, c);
            await RequireUniqueCode(request.Code, null, c);
            var item = await Security.StageCreateAsync(request.ToModel(), c);
            // Owned compatibility associations are written only after the owning type's authorization.
            await SynchronizeAllowedChildren(item.id, request.AllowedChildTypeIds, c);
            await Security.SaveChangesAsync(c);
            return await Response(item.id, c);
        }, ct);

    public Task<FacilityTypeResponse> UpdateAsync(FacilityTypeUpdateRequest request, CancellationToken ct = default)
        => FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            Validate(request.Code, request.Name, request.AllowedChildTypeIds);
            var item = await SecuredModels(SecurityOperations.Update).SingleOrDefaultAsync(x => x.id == request.id, c)
                ?? throw new UnauthorizedAccessException("The facility type is unavailable.");
            FacilitiesWriteBoundary.Token(request.updated_at, item.updated_at);
            await RequireChildTypes(request.AllowedChildTypeIds, c);
            await RequireUniqueCode(request.Code, item.id, c);
            // Global probes prevent making any existing parent/child relationship invalid, including hidden rows.
            var allowed = request.AllowedChildTypeIds;
            var edges = from parent in db.Query<FacilityModel>()
                        join child in db.Query<FacilityModel>() on parent.id equals child.ParentId
                        where parent.FacilityTypeId == item.id
                        select child.FacilityTypeId;
            if ((!request.CanContainChildren && await edges.AnyAsync(c))
                || (allowed.Length > 0 && await edges.AnyAsync(x => !allowed.Contains(x), c)))
                throw new InvalidOperationException("The compatibility rule would invalidate existing facilities.");
            if (!request.Reservable && await (
                    from reservation in db.Query<ReservationModel>()
                    join facility in db.Query<FacilityModel>() on reservation.FacilityId equals facility.id
                    where facility.FacilityTypeId == item.id && reservation.Status == ReservationStatuses.Confirmed
                        && reservation.EndsAt > DateTimeOffset.UtcNow
                    select reservation.id).AnyAsync(c))
                throw new InvalidOperationException("The type has future confirmed reservations.");
            await Security.StageUpdateAsync(request.ApplyTo(item), request.updated_at, c);
            await SynchronizeAllowedChildren(item.id, request.AllowedChildTypeIds, c);
            await Security.SaveChangesAsync(c);
            return await Response(item.id, c);
        }, ct);

    public async Task DeleteAsync(Guid id, FacilityDeleteRequest request, CancellationToken ct = default)
        => await FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            var item = await SecuredModels(SecurityOperations.Delete).SingleOrDefaultAsync(x => x.id == id, c)
                ?? throw new UnauthorizedAccessException("The facility type is unavailable.");
            FacilitiesWriteBoundary.Token(request.updated_at, item.updated_at);
            if (await db.Query<FacilityModel>().AnyAsync(x => x.FacilityTypeId == id, c)
                || await context.Set<FacilityTypeAllowedChildModel>().AnyAsync(x => x.ChildTypeId == id && x.ParentTypeId != id, c))
                throw new InvalidOperationException("The facility type is in use.");
            // Removing this type's aggregate configuration is bound to its Delete permission.
            context.RemoveRange(await context.Set<FacilityTypeAllowedChildModel>().Where(x => x.ParentTypeId == id).ToListAsync(c));
            await Security.StageDeleteAsync<FacilityTypeModel>(id, c);
            await Security.SaveChangesAsync(c);
            return true;
        }, ct);

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Use DeleteAsync with an updated_at token.");

    public override async Task<TDTO> UpdateAsync<TUpdateRequest, TDTO>(TUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request is not FacilityTypeUpdateRequest typed)
            throw new NotSupportedException("Only FacilityTypeUpdateRequest is supported.");
        await UpdateAsync(typed, cancellationToken);
        var model = await SecuredModels(SecurityOperations.Update).Include(x => x.AllowedChildTypes)
            .SingleAsync(x => x.id == typed.id, cancellationToken);
        return TDTO.FromModel(model);
    }

    private Task<FacilityTypeResponse> Response(Guid id, CancellationToken ct)
        => Unsecured_Query().Where(x => x.id == id).Select(FacilityTypeResponse.Projection).SingleAsync(ct);

    private async Task RequireUniqueCode(string code, Guid? except, CancellationToken ct)
    {
        if (await Unsecured_Query().AnyAsync(x => x.Code == code && x.id != except, ct))
            throw new InvalidOperationException("The facility type code is unavailable.");
    }

    private async Task RequireChildTypes(Guid[] ids, CancellationToken ct)
    {
        if (await SecuredModels(SecurityOperations.Read).CountAsync(x => ids.Contains(x.id), ct) != ids.Length)
            throw new UnauthorizedAccessException("One or more allowed child types are unavailable.");
    }

    private async Task SynchronizeAllowedChildren(Guid id, Guid[] ids, CancellationToken ct)
    {
        // Narrow aggregate write: parent Create/Update was authorized and staged in this transaction.
        // The same global lock covers all type and tree writes.
        var old = await context.Set<FacilityTypeAllowedChildModel>().Where(x => x.ParentTypeId == id).ToListAsync(ct);
        context.RemoveRange(old.Where(x => !ids.Contains(x.ChildTypeId)));
        var existing = old.Select(x => x.ChildTypeId).ToHashSet();
        context.AddRange(ids.Where(x => !existing.Contains(x)).Select(x => new FacilityTypeAllowedChildModel { ParentTypeId = id, ChildTypeId = x }));
    }

    private static void Validate(string code, string name, Guid[]? ids)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Code and Name are required.");
        if (ids is null || ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("AllowedChildTypeIds must contain distinct, nonempty identifiers.");
    }
}
