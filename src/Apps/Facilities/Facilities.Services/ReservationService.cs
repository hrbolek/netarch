using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Events;

namespace WebAppArchitecture.Facilities;

public sealed class ReservationService : SecuredEntityService<ReservationModel>, IReservationService
{
    private readonly IDBLayer db;
    private readonly CoreDbContext context;
    private readonly EventService events;

    public ReservationService(IAuthorizationLayer security, IDBLayer db, CoreDbContext context, EventService events)
        : base(security, db, new ReservationSecurityQueryProvider(security))
    {
        this.db = db;
        this.context = context;
        this.events = events;
    }

    public Task<ReservationResponse> RequestAsync(ReservationRequest request, CancellationToken ct = default)
        => FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            FacilitiesWriteBoundary.Range(request.StartsAt, request.EndsAt);
            await LockFacility(request.FacilityId, c);
            await FacilitiesWriteBoundary.RequireScopeAsync(Security, db, request.RbacObjectId, c);
            var facility = await RequireFacility(request.FacilityId, SecurityOperations.Read, c);
            var ev = await RequireEvent(request.EventId, SecurityOperations.Update, c);
            await CheckReservable(facility, c);
            CheckEventBounds(request.StartsAt, request.EndsAt, ev);
            // Pending requests deliberately do not consume occupancy.
            var item = await Security.StageCreateAsync(request.ToModel(), c);
            await Security.SaveChangesAsync(c);
            return ReservationResponse.FromModel(item);
        }, ct);

    public Task<ReservationResponse> ConfirmAsync(Guid id, ReservationTransitionRequest request, CancellationToken ct = default)
        => Transition(id, request, ReservationStatuses.Confirmed, ct);
    public Task<ReservationResponse> RejectAsync(Guid id, ReservationTransitionRequest request, CancellationToken ct = default)
        => Transition(id, request, ReservationStatuses.Rejected, ct);
    public Task<ReservationResponse> CancelAsync(Guid id, ReservationTransitionRequest request, CancellationToken ct = default)
        => Transition(id, request, ReservationStatuses.Cancelled, ct);

    private Task<ReservationResponse> Transition(Guid id, ReservationTransitionRequest request, string target, CancellationToken ct)
        => FacilitiesWriteBoundary.ExecuteAsync(Security, context, async c =>
        {
            // Private command lookup: public read composition always requires
            // related Read, but the approved transition policy requires Update only.
            var item = await Security.Query<ReservationModel>(SecurityOperations.Update).SingleOrDefaultAsync(x => x.id == id, c)
                ?? throw new UnauthorizedAccessException("The reservation is unavailable.");
            await LockFacility(item.FacilityId, c);
            FacilitiesWriteBoundary.Token(request.updated_at, item.updated_at);
            var facility = await RequireFacility(item.FacilityId, SecurityOperations.Update, c);
            var ev = await RequireEvent(item.EventId, SecurityOperations.Update, c);
            if (item.Status != ReservationStatuses.Requested && !(target == ReservationStatuses.Cancelled && item.Status == ReservationStatuses.Confirmed))
                throw new InvalidOperationException("The reservation cannot make this status transition.");
            if (target == ReservationStatuses.Confirmed)
            {
                await CheckReservable(facility, c);
                FacilitiesWriteBoundary.Range(item.StartsAt, item.EndsAt);
                CheckEventBounds(item.StartsAt, item.EndsAt, ev);
                // Privileged conflict probe sees hidden reservations but never returns their identities.
                // PostgreSQL's per-facility advisory lock is held through check, write and commit.
                if (await Conflicts(item.FacilityId, item.StartsAt, item.EndsAt, item.id).AnyAsync(c))
                    throw new InvalidOperationException("The facility is not available for this interval.");
            }
            item.Status = target; item.DecisionReason = request.Reason;
            var updated = await Security.StageUpdateAsync(item, request.updated_at, c);
            await Security.SaveChangesAsync(c);
            return ReservationResponse.FromModel(updated);
        }, ct);

    public async Task<IReadOnlyList<ReservationResponse>> ListByFacilityAsync(Guid facilityId, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        if (!await SecuredRelated<FacilityModel>(SecurityOperations.Read).AnyAsync(x => x.id == facilityId, ct)) return [];
        return await Page(ReadableReservations().Where(x => x.FacilityId == facilityId), skip, take).Select(ReservationResponse.Projection).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReservationResponse>> ListByEventAsync(Guid eventId, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        if (await events.FindAsync(eventId, ct, SecurityOperations.Read) is null) return [];
        return await Page(ReadableReservations().Where(x => x.EventId == eventId), skip, take).Select(ReservationResponse.Projection).ToListAsync(ct);
    }

    /// <summary>
    /// Association history across all reservation statuses, not current occupancy.
    /// The secured reservation source independently intersects Reservation, Facility
    /// and Event permissions before the distinct facility IDs are projected in SQL.
    /// </summary>
    public async Task<IReadOnlyList<FacilityResponse>> ListFacilitiesByEventAsync(
        Guid eventId, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var facilityIds = ReadableReservations().Where(x => x.EventId == eventId)
            .Select(x => x.FacilityId).Distinct();
        return await SecuredRelated<FacilityModel>(SecurityOperations.Read)
            .Where(x => facilityIds.Contains(x.id))
            .OrderBy(x => x.Name).ThenBy(x => x.id)
            .Skip(FacilitiesWriteBoundary.Skip(skip)).Take(FacilitiesWriteBoundary.Take(take))
            .Select(FacilityResponse.Projection).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReservationResponse>> OccupancyAsync(Guid facilityId, DateTimeOffset start, DateTimeOffset end, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        FacilitiesWriteBoundary.Range(start, end);
        if (!await SecuredRelated<FacilityModel>(SecurityOperations.Read).AnyAsync(x => x.id == facilityId, ct)) return [];
        start = start.ToUniversalTime(); end = end.ToUniversalTime();
        return await Page(ReadableReservations().Where(x => x.FacilityId == facilityId && x.Status == ReservationStatuses.Confirmed
                && x.StartsAt < end && start < x.EndsAt), skip, take)
            .Select(ReservationResponse.Projection).ToListAsync(ct);
    }

    public async Task<FacilityAvailabilityResponse> AvailabilityAsync(Guid facilityId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
    {
        FacilitiesWriteBoundary.Range(start, end);
        var facility = await RequireFacility(facilityId, SecurityOperations.Read, ct);
        // Only availability, not protected reservation/event information, is disclosed to facility readers.
        var reservable = await db.Query<FacilityTypeModel>().AnyAsync(x => x.id == facility.FacilityTypeId && x.Reservable, ct);
        start = start.ToUniversalTime(); end = end.ToUniversalTime();
        var available = facility.IsActive && reservable && !await Conflicts(facilityId, start, end, null).AnyAsync(ct);
        return new(facilityId, start, end, available);
    }

    private IQueryable<ReservationModel> ReadableReservations() => SecuredModels(SecurityOperations.Read);

    private static IQueryable<ReservationModel> Page(IQueryable<ReservationModel> query, int? skip, int? take)
        => query.OrderBy(x => x.StartsAt).ThenBy(x => x.id).Skip(FacilitiesWriteBoundary.Skip(skip)).Take(FacilitiesWriteBoundary.Take(take));

    private IQueryable<ReservationModel> Conflicts(Guid facilityId, DateTimeOffset start, DateTimeOffset end, Guid? except)
        => Unsecured_Query().Where(x => x.FacilityId == facilityId && x.Status == ReservationStatuses.Confirmed
            && x.id != except && x.StartsAt < end && start < x.EndsAt);

    private async Task<FacilityModel> RequireFacility(Guid id, string operation, CancellationToken ct)
    {
        return await SecuredRelated<FacilityModel>(operation).SingleOrDefaultAsync(x => x.id == id, ct)
            ?? throw new UnauthorizedAccessException("The facility is unavailable.");
    }

    private async Task<EventModel> RequireEvent(Guid id, string operation, CancellationToken ct)
    {
        // Actual Events service is the related owner; no mirrored Events persistence or reverse dependency.
        var ev = await events.FindAsync(id, ct, operation)
            ?? throw new UnauthorizedAccessException("The event is unavailable.");
        if (Security.HasActiveTransaction && context.Database.IsNpgsql())
        {
            // Prevent a concurrent Events update from changing bounds before this transaction commits.
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM events WHERE id = {id} FOR SHARE", ct);
            ev = await events.FindAsync(id, ct, operation)
                ?? throw new UnauthorizedAccessException("The event is unavailable.");
        }
        return ev;
    }

    private async Task CheckReservable(FacilityModel facility, CancellationToken ct)
    {
        // Type capability is an invariant; no protected type labels are returned.
        if (!facility.IsActive || !await db.Query<FacilityTypeModel>().AnyAsync(x => x.id == facility.FacilityTypeId && x.Reservable, ct))
            throw new InvalidOperationException("The facility is not active and reservable.");
    }

    private static void CheckEventBounds(DateTimeOffset start, DateTimeOffset end, EventModel ev)
    {
        if (start < ev.StartsAt || end > ev.EndsAt)
            throw new ArgumentException("The reservation interval must fit within the event interval.");
    }

    private Task<int> LockFacility(Guid id, CancellationToken ct)
    {
        // hash collisions only over-serialize; they cannot permit a conflict.
        var key = BitConverter.ToInt64(id.ToByteArray(), 0);
        return context.Database.IsNpgsql()
            ? context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", ct)
            : Task.FromResult(0);
    }

    public override Task<TDTO> UpdateAsync<TUpdateRequest, TDTO>(TUpdateRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Reservations can only change through Confirm, Reject or Cancel.");
    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Reservation history cannot be deleted; use Cancel.");
}
