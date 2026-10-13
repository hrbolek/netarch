using WebAppArchitecture.Base.Data;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
namespace WebAppArchitecture.Events;

/// <summary>
/// RBAC-safe access to events. Reads are filtered in SQL and paged; writes go through the security layer
/// (Stage* + SaveChanges in a transaction), which authorizes every change. The acting user is the current security identity.
/// Only <see cref="RegisterAsync"/> and <see cref="RespondAsync"/> are self-service and use the unsecured layer on purpose.
/// </summary>
public sealed class EventService(ISecurityLayer securityLayer, IDBLayer unsecuredDb, CoreDbContext catalogDb, EventTypeService types)
 : TreeSecuredService<EventModel>(securityLayer, unsecuredDb)
{
    protected override IQueryable<EventModel> Order(IQueryable<EventModel> q)=>q.OrderBy(x=>x.StartsAt).ThenBy(x=>x.id);
 public Task<bool> CanAsync(EventModel item, string operation, CancellationToken ct) => Security.IsAllowedAsync(item.rbacobject_id, operation, ct);

    public Task<List<EventModel>> ListAsync(string? where,int? skip,int? take,CancellationToken ct)
  => Page(EventLookupFilter.Where(SecuredModels(SecurityOperations.Read),where,x=>x.Name),skip,take).ToListAsync(ct);
 public Task<EventModel?> FindAsync(Guid id,CancellationToken ct,string operation=SecurityOperations.Read)
  => SecuredModels(operation).Where(x=>x.id==id).FirstOrDefaultAsync(ct);
 public async Task<List<EventModel>> PlannerSegmentsAsync(Guid yearId,CancellationToken ct)
 {
  var path=await PathOfAsync(yearId,SecurityOperations.Read,ct);
  return path is null?new():await Order(SecuredChildren(path,SecurityOperations.Read).Where(x=>x.IsPlannerSegment)).Take(600).ToListAsync(ct);
 }

 public async Task<EventModel?> CreateAsync(EventCreateRequest request, CancellationToken ct)
    {
        if (request.StartsAt >= request.EndsAt) return null;
        if (!await types.ExistsReadableAsync(request.EventTypeId, ct)) return null;
        Guid scope;
        if (request.ParentId is Guid pid)
        {
            var parent = await FindAsync(pid, ct, SecurityOperations.Create);
            if (parent is null) return null;
            scope = parent.rbacobject_id;
        }
        else
        {
            scope = request.RbacObjectId ?? Guid.Empty;
            if (scope == Guid.Empty || !await Security.IsAllowedAsync(scope, SecurityOperations.Create, ct)) return null;
        }
        var actor = Security.CurrentUserId; var now = DateTimeOffset.UtcNow;
        var item = new EventModel { Name = request.Name, Description = request.Description, EventTypeId = request.EventTypeId, ParentId = request.ParentId, StartsAt = request.StartsAt.ToUniversalTime(), EndsAt = request.EndsAt.ToUniversalTime(), rbacobject_id = scope, created_by = actor, updated_by = actor, created_at = now, updated_at = now };
        return await Security.ExecuteInTransactionAsync(async c =>
        {
            var entity = await Security.StageCreateAsync(item, c);
            await Security.SaveChangesAsync(c);
            return entity;
        }, ct);
    }

    public async Task<bool> UpdateAsync(Guid id, EventUpdateRequest request, CancellationToken ct)
    {
        var item = await FindAsync(id, ct, SecurityOperations.Update);
        if (item is null || request.StartsAt >= request.EndsAt || await CreatesCycleAsync(id, request.ParentId, ct)) return false;
        if (!await types.ExistsReadableAsync(request.EventTypeId, ct)) return false;
        if (request.ParentId is Guid newPid && newPid != item.ParentId)
        {
            // Moving under another parent requires Update on that parent (it must also be visible to the actor).
            if (await FindAsync(newPid, ct, SecurityOperations.Update) is null) return false;
        }
        item.Name = request.Name; item.Description = request.Description; item.EventTypeId = request.EventTypeId; item.ParentId = request.ParentId; item.StartsAt = request.StartsAt.ToUniversalTime(); item.EndsAt = request.EndsAt.ToUniversalTime(); item.updated_by = Security.CurrentUserId; item.updated_at = DateTimeOffset.UtcNow;
        await Security.ExecuteInTransactionAsync(async c => { await Security.StageUpdateAsync(item, c); await Security.SaveChangesAsync(c); }, ct);
        return true;
    }

    public Task<bool> TryDeleteAsync(Guid id,CancellationToken ct)=>TryDeleteLeafAsync(id,ct);

 public async Task<bool> PrivilegedStatusAsync(Guid id, Guid status, Guid expected, CancellationToken ct)
    {
        var participant = await SecuredRelated<EventParticipantModel>(SecurityOperations.Update).Where(x => x.id == id).FirstOrDefaultAsync(ct);
        if (participant is null || participant.StatusId != expected) return false;
        if (await FindAsync(participant.EventId, ct, SecurityOperations.Update) is null) return false;
        var now = DateTimeOffset.UtcNow;
        participant.StatusId = status; participant.updated_by = Security.CurrentUserId; participant.updated_at = now; participant.StatusChangedAt = now;
        await Security.ExecuteInTransactionAsync(async c => { await Security.StageUpdateAsync(participant, c); await Security.SaveChangesAsync(c); }, ct);
        return true;
    }

    public async Task<bool> SaveAcademicPlanAsync(Guid academicYearId, AcademicPlanSaveRequest request, CancellationToken ct)
    {
        var year = await FindAsync(academicYearId, ct, SecurityOperations.Update);
        if (year is null) return false;
        // Only direct planner segments are editable. Ordinary child events remain untouched.
        if (request.Segments.Count > 600 || request.Segments.Any(s => s.EndsAt <= s.StartsAt)) return false;
        var supplied = request.Segments.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();
        if (supplied.Distinct().Count() != supplied.Count) return false;
        var typeIds = request.Segments.Select(s => s.EventTypeId).Distinct().ToArray();
        if (typeIds.Length > 0 && await SecuredRelated<EventTypeModel>(SecurityOperations.Read).CountAsync(t => typeIds.Contains(t.id), ct) != typeIds.Length) return false;
        var actor = Security.CurrentUserId;
        return await Security.ExecuteInTransactionAsync(async c =>
        {
            // Re-read inside the transaction: optimistic concurrency on the academic year.
            var fresh = await FindAsync(academicYearId, c, SecurityOperations.Update);
            if (fresh is null || fresh.updated_at != request.ExpectedUpdatedAt) return false;
            var current = await SecuredModels(SecurityOperations.Update).Where(x => x.ParentId == academicYearId && x.IsPlannerSegment).ToListAsync(c);
            var existing = current.ToDictionary(x => x.id);
            if (supplied.Any(i => !existing.ContainsKey(i))) return false;
            var retained = supplied.ToHashSet();
            foreach (var old in current.Where(x => !retained.Contains(x.id))) await Security.StageDeleteAsync<EventModel>(old.id, c);
            var now = DateTimeOffset.UtcNow;
            foreach (var segment in request.Segments)
            {
                if (segment.Id is Guid id)
                {
                    var entity = existing[id]; entity.Name = segment.Name; entity.EventTypeId = segment.EventTypeId; entity.StartsAt = segment.StartsAt.ToUniversalTime(); entity.EndsAt = segment.EndsAt.ToUniversalTime(); entity.updated_at = now; entity.updated_by = actor;
                    await Security.StageUpdateAsync(entity, c);
                }
                else await Security.StageCreateAsync(new EventModel { Name = segment.Name, ParentId = academicYearId, EventTypeId = segment.EventTypeId, StartsAt = segment.StartsAt.ToUniversalTime(), EndsAt = segment.EndsAt.ToUniversalTime(), IsPlannerSegment = true, rbacobject_id = fresh.rbacobject_id, created_by = actor, updated_by = actor, created_at = now, updated_at = now }, c);
            }
            fresh.updated_at = now; fresh.updated_by = actor;
            await Security.StageUpdateAsync(fresh, c);
            await Security.SaveChangesAsync(c);
            return true;
        }, ct);
    }

    public async Task<EventRegistrationPolicyModel?> UpsertPolicyAsync(Guid eventId, RegistrationPolicyRequest req, CancellationToken ct)
    {
        var ev = await FindAsync(eventId, ct, SecurityOperations.Update);
        if (ev is null || req.Capacity is < 0 || req.OpensAt >= req.ClosesAt) return null;
        var actor = Security.CurrentUserId; var now = DateTimeOffset.UtcNow;
        var policy = await unsecuredDb.Query<EventRegistrationPolicyModel>().Where(x => x.EventId == eventId).SingleOrDefaultAsync(ct);
        var isNew = policy is null;
        policy ??= new EventRegistrationPolicyModel { EventId = eventId, rbacobject_id = ev.rbacobject_id, created_by = actor, created_at = now };
        policy.Capacity = req.Capacity; policy.OpensAt = req.OpensAt?.ToUniversalTime(); policy.ClosesAt = req.ClosesAt?.ToUniversalTime(); policy.updated_by = actor; policy.updated_at = now;
        return await Security.ExecuteInTransactionAsync(async c =>
        {
            var saved = isNew ? await Security.StageCreateAsync(policy, c) : await Security.StageUpdateAsync(policy, c);
            await Security.SaveChangesAsync(c);
            return saved;
        }, ct);
    }

    public async Task<EventParticipantModel?> InviteAsync(Guid eventId, ParticipantAssignmentRequest req, CancellationToken ct)
    {
        var ev = await FindAsync(eventId, ct, SecurityOperations.Update);
        if (ev is null) return null;
        if ((req.UserId.HasValue) == (req.GroupId.HasValue)) return null;
        if (req.GroupId is Guid group && !await unsecuredDb.Query<GroupModel>().AnyAsync(x => x.id == group, ct)) return null;
        if (req.UserId is Guid user && !await unsecuredDb.Query<UserModel>().AnyAsync(x => x.id == user, ct)) return null;
        if (await unsecuredDb.Query<EventParticipantModel>().AnyAsync(x => x.EventId == eventId && (req.UserId.HasValue ? x.UserId == req.UserId : x.GroupId == req.GroupId), ct)) return null;
        var actor = Security.CurrentUserId; var now = DateTimeOffset.UtcNow;
        var item = new EventParticipantModel { EventId = eventId, UserId = req.UserId, GroupId = req.GroupId, SourceId = req.Source == "assignment" ? EventKnownIds.Assignment : EventKnownIds.Invitation, StatusId = req.Source == "assignment" ? EventKnownIds.Accepted : EventKnownIds.Invited, RoleId = req.RoleId ?? EventKnownIds.Participant, rbacobject_id = ev.rbacobject_id, created_at = now, updated_at = now, created_by = actor, updated_by = actor, StatusChangedAt = now };
        if (!await catalogDb.Set<EventParticipantRoleModel>().AnyAsync(x => x.id == item.RoleId, ct)) return null;
        return await Security.ExecuteInTransactionAsync(async c =>
        {
            var entity = await Security.StageCreateAsync(item, c);
            await Security.SaveChangesAsync(c);
            return entity;
        }, ct);
    }

    public async Task<List<EventParticipantModel>?> ParticipantsAsync(Guid eventId, CancellationToken ct)
    {
        if (await FindAsync(eventId, ct) is null) return null;
        return await SecuredRelated<EventParticipantModel>(SecurityOperations.Read).Where(x => x.EventId == eventId).OrderBy(x => x.created_at).ThenBy(x => x.id).Take(MaxPageSize * 5).ToListAsync(ct);
    }

    /// <summary>Self-service registration of the current user; authorization is the registration policy, not RBAC on the event.</summary>
    public async Task<EventParticipantModel?> RegisterAsync(Guid eventId, CancellationToken ct)
    {
        var userId = Security.CurrentUserId;
        var item = await Unsecured_ReadAsync(eventId, ct);
        if (item is null || userId == Guid.Empty) return null;
        var policy = await unsecuredDb.Query<EventRegistrationPolicyModel>().Where(x => x.EventId == eventId).SingleOrDefaultAsync(ct);
        var mode = await types.ResolveModeAsync(item.EventTypeId, ct);
        if (mode is not ("open" or "approval_required") || policy is null) return null;
        var now = DateTimeOffset.UtcNow;
        if (policy.OpensAt is not null && now < policy.OpensAt || policy.ClosesAt is not null && now >= policy.ClosesAt) return null;
        var participants = unsecuredDb.Query<EventParticipantModel>();
        if (await participants.AnyAsync(x => x.EventId == eventId && x.UserId == userId, ct)) return null;
        if (policy.Capacity is int capacity && await participants.CountAsync(x => x.EventId == eventId && (x.StatusId == EventKnownIds.Accepted || x.StatusId == EventKnownIds.Attended), ct) >= capacity) return null;
        var participant = new EventParticipantModel { EventId = eventId, UserId = userId, SourceId = EventKnownIds.SelfRegistration, StatusId = mode == "open" ? EventKnownIds.Accepted : EventKnownIds.PendingApproval, RoleId = EventKnownIds.Participant, rbacobject_id = item.rbacobject_id, created_by = userId, updated_by = userId, RespondedAt = now, StatusChangedAt = now };
        return await unsecuredDb.ExecuteInTransactionAsync(async c =>
        {
            var entity = await unsecuredDb.StageCreateAsync(participant, c);
            await unsecuredDb.SaveChangesAsync(c);
            return entity;
        }, ct);
    }

    /// <summary>Self-service: the invited user accepts or declines their own invitation.</summary>
    public async Task<bool> RespondAsync(Guid participationId, bool accept, CancellationToken ct)
    {
        var userId = Security.CurrentUserId;
        var item = await unsecuredDb.Query<EventParticipantModel>().Where(x => x.id == participationId && x.UserId == userId).FirstOrDefaultAsync(ct);
        if (item is null || item.StatusId != EventKnownIds.Invited) return false;
        item.StatusId = accept ? EventKnownIds.Accepted : EventKnownIds.Declined;
        item.RespondedAt = DateTimeOffset.UtcNow; item.StatusChangedAt = item.RespondedAt; item.updated_at = item.RespondedAt.Value; item.updated_by = userId;
        return await unsecuredDb.ExecuteInTransactionAsync(async c => { await unsecuredDb.StageUpdateAsync(item, c); await unsecuredDb.SaveChangesAsync(c); return true; }, ct);
    }
}

public sealed record EventCreateRequest(string Name, string? Description, Guid EventTypeId, Guid? ParentId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, Guid? RbacObjectId);
public sealed record EventUpdateRequest(string Name, string? Description, Guid EventTypeId, Guid? ParentId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

public sealed record AcademicPlanSegment(Guid? Id, Guid EventTypeId, string Name, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
public sealed record AcademicPlanSaveRequest(DateTimeOffset ExpectedUpdatedAt, List<AcademicPlanSegment> Segments);

public sealed record RegistrationPolicyRequest(int? Capacity, DateTimeOffset? OpensAt, DateTimeOffset? ClosesAt);
public sealed record ParticipantAssignmentRequest(Guid? UserId, Guid? GroupId, string Source, Guid? RoleId);
