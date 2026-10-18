using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Facilities;

public sealed class FacilityService : TreeSecuredService<FacilityModel>, IFacilityService
{
    private readonly IDBLayer db;
    private readonly CoreDbContext context;
    private readonly FacilityPermissionTree permissionTree;

    public FacilityService(IAuthorizationLayer security, IDBLayer db, CoreDbContext context,
        FacilityPermissionTree permissionTree) : base(security, db)
    {
        this.db = db;
        this.context = context;
        this.permissionTree = permissionTree;
    }

    protected override IQueryable<FacilityModel> Order(IQueryable<FacilityModel> query)
        => query.OrderBy(x => x.Name).ThenBy(x => x.id);

    public async Task<IReadOnlyList<FacilityResponse>> ListAsync(string? search = null, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var query = SecuredModels(SecurityOperations.Read);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        return await Page(query, skip, take).Select(FacilityResponse.Projection).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FacilityResponse>> ListChildrenAsync(Guid parentId, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var path = await PathOfAsync(parentId, SecurityOperations.Read, ct);
        return path is null ? [] : await Page(SecuredChildren(path, SecurityOperations.Read), skip, take).Select(FacilityResponse.Projection).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FacilityResponse>> ListDescendantsAsync(Guid id, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var path = await PathOfAsync(id, SecurityOperations.Read, ct);
        return path is null ? [] : await SecuredSubtree(path, SecurityOperations.Read).OrderBy(x => x.path)
            .Skip(FacilitiesWriteBoundary.Skip(skip)).Take(FacilitiesWriteBoundary.Take(take))
            .Select(FacilityResponse.Projection).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FacilityResponse>> ListAncestorsAsync(Guid id, CancellationToken ct = default)
    {
        var path = await PathOfAsync(id, SecurityOperations.Read, ct);
        if (path is null) return [];
        var ids = TreePath.AncestorIds(path).ToArray();
        return await SecuredModels(SecurityOperations.Read).Where(x => ids.Contains(x.id)).OrderBy(x => x.path.Length)
            .Select(FacilityResponse.Projection).ToListAsync(ct);
    }

    public Task<FacilityResponse> CreateAsync(FacilityCreateRequest request, CancellationToken ct = default)
        => FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            await permissionTree.LockAsync(c);
            ValidateFields(request.Name, request.Code, request.Capacity);
            await RequireReadableType(request.FacilityTypeId, c);
            var parentScope = request.RbacObjectId;
            var parentPath = "";
            if (request.ParentId is Guid parentId)
            {
                var parent = await RequireNode(parentId, SecurityOperations.Create, c);
                await CheckCompatibility(parent.FacilityTypeId, request.FacilityTypeId, c);
                parentScope = parent.rbacobject_id;
                parentPath = parent.path;
            }
            var group = await permissionTree.CreateOwnedGroupAsync(request.Name, parentScope, c);
            var model = request.ToModel();
            model.rbacobject_id = group.id;
            model.path = request.ParentId.HasValue ? TreePath.Child(parentPath, model.id) : TreePath.Root(model.id);
            // Narrow owned-group command: Create was authorized on the existing
            // parent; requiring Create on the newly minted scope would be incorrect.
            var item = await db.StageAuditedCreateAsync(model, Security.CurrentUserId, c);
            await Security.SaveChangesAsync(c);
            return FacilityResponse.FromModel(item);
        }, ct);

    public Task<FacilityResponse> UpdateAsync(FacilityUpdateRequest request, CancellationToken ct = default)
        => FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            ValidateFields(request.Name, request.Code, request.Capacity);
            var item = await RequireNode(request.id, SecurityOperations.Update, c);
            FacilitiesWriteBoundary.Token(request.updated_at, item.updated_at);
            await RequireReadableType(request.FacilityTypeId, c);
            // Unfiltered sources below check only global structural invariants; they return no protected data.
            if (item.ParentId is Guid pid)
            {
                var parentType = await Unsecured_Query().Where(x => x.id == pid).Select(x => x.FacilityTypeId).SingleAsync(c);
                await CheckCompatibility(parentType, request.FacilityTypeId, c);
            }
            foreach (var childType in await Unsecured_Query().Where(x => x.ParentId == item.id).Select(x => x.FacilityTypeId).Distinct().ToListAsync(c))
                await CheckCompatibility(request.FacilityTypeId, childType, c);
            var changingToNonreservableType = request.FacilityTypeId != item.FacilityTypeId
                && !await db.Query<FacilityTypeModel>().AnyAsync(x => x.id == request.FacilityTypeId && x.Reservable, c);
            if ((!request.IsActive || changingToNonreservableType)
                && await db.Query<ReservationModel>().AnyAsync(x => x.FacilityId == item.id
                    && x.Status == ReservationStatuses.Confirmed && x.EndsAt > DateTimeOffset.UtcNow, c))
                throw new InvalidOperationException("A facility with future confirmed reservations cannot be deactivated or assigned a nonreservable type.");
            var updated = await Security.StageUpdateAsync(request.ApplyTo(item), request.updated_at, c);
            await Security.SaveChangesAsync(c);
            return FacilityResponse.FromModel(updated);
        }, ct);

    public Task<FacilityResponse> MoveAsync(Guid id, FacilityMoveRequest request, CancellationToken ct = default)
        => FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            await permissionTree.LockAsync(c);
            if (request.ParentId is not Guid destinationId)
                throw new ArgumentException("A destination parent is required for a permission-tree move.");
            // Query(move) is unsupported by Core's operation-ID switch. Bind the
            // privileged source lookup immediately to the dedicated literal check.
            var root = await Unsecured_ReadAsync(id, c);
            if (root is null || !await Security.IsAllowedAsync(root.rbacobject_id, FacilityPermissionTree.MoveOperation, c))
                throw new UnauthorizedAccessException("The facility is unavailable for move.");
            FacilitiesWriteBoundary.Token(request.updated_at, root.updated_at);
            var destination = await RequireNode(destinationId, SecurityOperations.Create, c);
            if (TreePath.ContainsNode(destination.path, id))
                throw new InvalidOperationException("Hierarchy cannot contain a cycle.");
            await CheckCompatibility(destination.FacilityTypeId, root.FacilityTypeId, c);
            var oldPath = root.path;
            var newPath = TreePath.Child(destination.path, root.id);
            return await permissionTree.CopyForMoveAsync(root.rbacobject_id, destination.rbacobject_id, async (map, token) =>
            {
                var scopes = map.Keys.ToArray();
                // Permission and physical Facility subtrees must match in both
                // directions. Never rebind unrelated Facilities merely because
                // their scopes happen to lie inside the copied Group subtree.
                var affected = await Unsecured_Query()
                    .Where(x => scopes.Contains(x.rbacobject_id) || x.path.StartsWith(oldPath)).ToListAsync(token);
                if (affected.Any(x => x.path.StartsWith(oldPath) && !map.ContainsKey(x.rbacobject_id)))
                    throw new InvalidOperationException("Physical and permission subtrees are inconsistent.");
                if (affected.Any(x => map.ContainsKey(x.rbacobject_id)
                    && !x.path.StartsWith(oldPath, StringComparison.Ordinal)))
                    throw new InvalidOperationException("The permission subtree includes an unrelated facility.");
                FacilityModel? moved = null;
                foreach (var item in affected)
                {
                    var expected = item.updated_at;
                    if (!map.TryGetValue(item.rbacobject_id, out var scope))
                        throw new InvalidOperationException("A copied permission scope is missing.");
                    item.rbacobject_id = scope;
                    if (item.path.StartsWith(oldPath, StringComparison.Ordinal))
                        item.path = newPath + item.path[oldPath.Length..];
                    if (item.id == id) item.ParentId = destinationId;
                    // Dedicated Move authority permits only scope/path reassociation;
                    // audited persistence preserves creation metadata and tokens.
                    var original = await db.GetByIdAsync<FacilityModel>(item.id, token)
                        ?? throw new InvalidOperationException("A facility disappeared during move.");
                    var staged = await db.StageAuditedUpdateAsync(item, original, expected, Security.CurrentUserId, token);
                    if (item.id == id) moved = staged;
                }
                return FacilityResponse.FromModel(moved ?? throw new InvalidOperationException("Moved root is missing."));
            }, c);
        }, ct);

    public async Task DeleteAsync(Guid id, FacilityDeleteRequest request, CancellationToken ct = default)
        => await FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            var item = await RequireNode(id, SecurityOperations.Delete, c);
            FacilitiesWriteBoundary.Token(request.updated_at, item.updated_at);
            if (await HasDescendantsAsync(id, c) || await db.Query<ReservationModel>().AnyAsync(x => x.FacilityId == id, c))
                throw new InvalidOperationException("Only a leaf without reservation history can be deleted.");
            await Security.StageDeleteAsync<FacilityModel>(id, c);
            await Security.SaveChangesAsync(c);
            return true;
        }, ct);

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Use DeleteAsync with an updated_at token.");

    public override async Task<TDTO> UpdateAsync<TUpdateRequest, TDTO>(TUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request is not FacilityUpdateRequest typed)
            throw new NotSupportedException("Only FacilityUpdateRequest is supported.");
        var response = await UpdateAsync(typed, cancellationToken);
        // Read back only the approved projection selected by the caller, after domain validation.
        var model = await Security.GetByIdAsync<FacilityModel>(response.id, SecurityOperations.Update, cancellationToken)
            ?? throw new UnauthorizedAccessException();
        return TDTO.FromModel(model);
    }

    private async Task<FacilityModel> RequireNode(Guid id, string operation, CancellationToken ct)
        => await SecuredModels(operation).SingleOrDefaultAsync(x => x.id == id, ct)
            ?? throw new UnauthorizedAccessException("The facility is unavailable.");

    private async Task RequireReadableType(Guid id, CancellationToken ct)
    {
        if (!await SecuredRelated<FacilityTypeModel>(SecurityOperations.Read).AnyAsync(x => x.id == id, ct))
            throw new UnauthorizedAccessException("The facility type is unavailable.");
    }

    private async Task CheckCompatibility(Guid parentTypeId, Guid childTypeId, CancellationToken ct)
    {
        var type = await db.Query<FacilityTypeModel>().Where(x => x.id == parentTypeId)
            .Select(x => new { x.CanContainChildren, Restricted = x.AllowedChildTypes.Any(), Allowed = x.AllowedChildTypes.Any(a => a.ChildTypeId == childTypeId) }).SingleAsync(ct);
        if (!type.CanContainChildren || (type.Restricted && !type.Allowed))
            throw new InvalidOperationException("The facility types are not compatible.");
    }

    private static void ValidateFields(string name, string code, int? capacity)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.");
        if (capacity < 0) throw new ArgumentException("Capacity must be null or nonnegative.");
    }
}
