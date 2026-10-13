using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
namespace WebAppArchitecture.Events;
public sealed class EventService(CoreDbContext db, SecurityService security)
{
 public Task<bool> CanTypeAsync(Guid actor,EventTypeModel type,string op,CancellationToken ct) => security.IsAllowedAsync(actor,type.rbacobject_id,op,ct);
 public async Task<EventModel?> CreateAsync(EventCreateRequest request,Guid actor,CancellationToken ct)
 {
  if(request.StartsAt>=request.EndsAt)return null;
  if(!await db.Set<EventTypeModel>().AnyAsync(x=>x.id==request.EventTypeId,ct))return null;
  if(request.ParentId is Guid parentId && !await db.Set<EventModel>().AnyAsync(x=>x.id==parentId,ct))return null;
  // For root events caller must have Create on the explicit RBAC security scope.
  var scope=request.ParentId is Guid pid ? await db.Set<EventModel>().Where(x=>x.id==pid).Select(x=>(Guid?)x.rbacobject_id).FirstOrDefaultAsync(ct):request.RbacObjectId;
  if(scope is null || scope==Guid.Empty || !await security.IsAllowedAsync(actor,scope.Value,"create",ct))return null;
  var now=DateTimeOffset.UtcNow;
  var item=new EventModel {Name=request.Name,Description=request.Description,EventTypeId=request.EventTypeId,ParentId=request.ParentId,StartsAt=request.StartsAt.ToUniversalTime(),EndsAt=request.EndsAt.ToUniversalTime(),rbacobject_id=scope.Value,created_by=actor,updated_by=actor,created_at=now,updated_at=now};
  db.Set<EventModel>().Add(item);await db.SaveChangesAsync(ct);return item;
 }
 public async Task<bool> UpdateAsync(Guid id,EventUpdateRequest request,Guid actor,CancellationToken ct)
 {
  var item=await db.Set<EventModel>().FirstOrDefaultAsync(x=>x.id==id,ct);
  if(item is null || !await CanAsync(actor,item,"update",ct) || request.StartsAt>=request.EndsAt || await CreatesCycleAsync(id,request.ParentId,ct))return false;
  if(request.ParentId is Guid pid && !await db.Set<EventModel>().AnyAsync(x=>x.id==pid,ct))return false;
  if(!await db.Set<EventTypeModel>().AnyAsync(x=>x.id==request.EventTypeId,ct))return false;
  // Prevent moving protected descendants into a different scope without authorization.
  if(request.ParentId!=item.ParentId && request.ParentId is Guid newPid)
  {
   var parent=await db.Set<EventModel>().FirstAsync(x=>x.id==newPid,ct);
   if(!await CanAsync(actor,parent,"update",ct))return false;
  }
  item.Name=request.Name;item.Description=request.Description;item.EventTypeId=request.EventTypeId;item.ParentId=request.ParentId;item.StartsAt=request.StartsAt.ToUniversalTime();item.EndsAt=request.EndsAt.ToUniversalTime();item.updated_by=actor;item.updated_at=DateTimeOffset.UtcNow;
  await db.SaveChangesAsync(ct);return true;
 }
 public async Task<bool> DeleteAsync(Guid id,Guid actor,CancellationToken ct)
 {
  var item=await db.Set<EventModel>().FirstOrDefaultAsync(x=>x.id==id,ct);
  if(item is null || !await CanAsync(actor,item,"delete",ct) || await db.Set<EventModel>().AnyAsync(x=>x.ParentId==id,ct))return false;
  db.Set<EventModel>().Remove(item);await db.SaveChangesAsync(ct);return true;
 }
 public async Task<bool> PrivilegedStatusAsync(Guid id,Guid actor,Guid status,Guid expected,CancellationToken ct)
 {
  var participant=await db.Set<EventParticipantModel>().FirstOrDefaultAsync(x=>x.id==id,ct);
  if(participant is null || participant.StatusId!=expected)return false;
  var ev=await db.Set<EventModel>().FirstOrDefaultAsync(x=>x.id==participant.EventId,ct);
  if(ev is null || !await CanAsync(actor,ev,"update",ct))return false;
  participant.StatusId=status;participant.updated_by=actor;participant.updated_at=DateTimeOffset.UtcNow;participant.StatusChangedAt=participant.updated_at;
  await db.SaveChangesAsync(ct);return true;
 }
 public async Task<bool> SaveAcademicPlanAsync(Guid academicYearId,AcademicPlanSaveRequest request,Guid actor,CancellationToken ct)
 {
  var year=await db.Set<EventModel>().FirstOrDefaultAsync(e=>e.id==academicYearId,ct);
  if(year is null || !await CanAsync(actor,year,"update",ct))return false;
  // Only direct planner segments are editable. Ordinary child events remain untouched.
  if(request.Segments.Count>600 || request.Segments.Any(s=>s.EndsAt<=s.StartsAt))return false;
  var current=await db.Set<EventModel>().Where(x=>x.ParentId==academicYearId && x.IsPlannerSegment).ToListAsync(ct);
  foreach(var segment in current)if(!await security.IsAllowedAsync(actor,segment.rbacobject_id,"update",ct))return false;
  var existing=current.ToDictionary(x=>x.id);
  if(request.Segments.Where(s=>s.Id.HasValue).Select(s=>s.Id!.Value).Distinct().Count()!=request.Segments.Count(s=>s.Id.HasValue))return false;
  if(request.Segments.Any(s=>s.Id.HasValue&&!existing.ContainsKey(s.Id.Value)))return false;
  var typeIds=request.Segments.Select(s=>s.EventTypeId).Distinct().ToArray();
  var allowedTypes=await db.Set<EventTypeModel>().Where(t=>typeIds.Contains(t.id)).Select(t=>t.id).ToArrayAsync(ct);
  if(allowedTypes.Length!=typeIds.Length)return false;
  // Prevent broad automatic interval merges: only segments explicitly supplied by the caller are saved.
  await using var transaction=await db.Database.BeginTransactionAsync(ct);
  await db.Entry(year).ReloadAsync(ct);
  if(year.updated_at!=request.ExpectedUpdatedAt)return false;
  var retained=request.Segments.Where(s=>s.Id.HasValue).Select(s=>s.Id!.Value).ToHashSet();
  foreach(var old in current.Where(x=>!retained.Contains(x.id)))db.Set<EventModel>().Remove(old);
  var now=DateTimeOffset.UtcNow;
  foreach(var segment in request.Segments)
  {
   if(segment.Id is Guid id)
   {
    var entity=existing[id];entity.Name=segment.Name;entity.EventTypeId=segment.EventTypeId;entity.StartsAt=segment.StartsAt.ToUniversalTime();entity.EndsAt=segment.EndsAt.ToUniversalTime();entity.updated_at=now;entity.updated_by=actor;
   }
   else db.Set<EventModel>().Add(new EventModel {Name=segment.Name,ParentId=academicYearId,EventTypeId=segment.EventTypeId,StartsAt=segment.StartsAt.ToUniversalTime(),EndsAt=segment.EndsAt.ToUniversalTime(),IsPlannerSegment=true,rbacobject_id=year.rbacobject_id,created_by=actor,updated_by=actor,created_at=now,updated_at=now});
  }
  year.updated_at=now;year.updated_by=actor;
  await db.SaveChangesAsync(ct);await transaction.CommitAsync(ct);return true;
 }
 public async Task<EventTypeModel?> CreateTypeAsync(EventTypeCreateRequest req,Guid actor,CancellationToken ct)
 {
  if(string.IsNullOrWhiteSpace(req.Code)||string.IsNullOrWhiteSpace(req.Name))return null;
  var scope=req.RbacObjectId;
  if(req.ParentId is Guid parentId)
  {
   var parent=await db.Set<EventTypeModel>().FirstOrDefaultAsync(x=>x.id==parentId,ct);
   if(parent is null)return null;scope=parent.rbacobject_id;
  }
  if(scope==Guid.Empty||!await security.IsAllowedAsync(actor,scope,"create",ct))return null;
  if(await db.Set<EventTypeModel>().AnyAsync(x=>x.Code==req.Code,ct))return null;
  var now=DateTimeOffset.UtcNow;
  var item=new EventTypeModel{Code=req.Code,Name=req.Name,Description=req.Description,Abbreviation=req.Abbreviation,Color=req.Color,RegistrationMode=req.RegistrationMode,ParentId=req.ParentId,rbacobject_id=scope,created_at=now,updated_at=now,created_by=actor,updated_by=actor};
  db.Set<EventTypeModel>().Add(item);await db.SaveChangesAsync(ct);return item;
 }
 public async Task<bool> UpdateTypeAsync(Guid id,EventTypeUpdateRequest req,Guid actor,CancellationToken ct)
 {
  var item=await db.Set<EventTypeModel>().FirstOrDefaultAsync(x=>x.id==id,ct);
  if(item is null||!await CanTypeAsync(actor,item,"update",ct)||string.IsNullOrWhiteSpace(req.Code)||string.IsNullOrWhiteSpace(req.Name))return false;
  if(req.ParentId==id)return false;
  var visited=new HashSet<Guid>{id};var ancestor=req.ParentId;
  while(ancestor is Guid pid)
  {
   if(!visited.Add(pid))return false;
   var parent=await db.Set<EventTypeModel>().FirstOrDefaultAsync(x=>x.id==pid,ct);
   if(parent is null)return false;ancestor=parent.ParentId;
  }
  if(await db.Set<EventTypeModel>().AnyAsync(x=>x.Code==req.Code&&x.id!=id,ct))return false;
  item.Code=req.Code;item.Name=req.Name;item.Description=req.Description;item.Abbreviation=req.Abbreviation;item.Color=req.Color;item.RegistrationMode=req.RegistrationMode;item.ParentId=req.ParentId;item.updated_by=actor;item.updated_at=DateTimeOffset.UtcNow;
  await db.SaveChangesAsync(ct);return true;
 }
 public async Task<bool> DeleteTypeAsync(Guid id,Guid actor,CancellationToken ct)
 {
  var item=await db.Set<EventTypeModel>().FirstOrDefaultAsync(x=>x.id==id,ct);
  if(item is null||!await CanTypeAsync(actor,item,"delete",ct)||await db.Set<EventTypeModel>().AnyAsync(x=>x.ParentId==id,ct)||await db.Set<EventModel>().AnyAsync(x=>x.EventTypeId==id,ct))return false;
  db.Set<EventTypeModel>().Remove(item);await db.SaveChangesAsync(ct);return true;
 }
 public async Task<EventRegistrationPolicyModel?> UpsertPolicyAsync(Guid eventId,RegistrationPolicyRequest req,Guid actor,CancellationToken ct)
 {
  var ev=await db.Set<EventModel>().FirstOrDefaultAsync(x=>x.id==eventId,ct);
  if(ev is null||!await CanAsync(actor,ev,"update",ct)||req.Capacity is <0||req.OpensAt>=req.ClosesAt)return null;
  var policy=await db.Set<EventRegistrationPolicyModel>().SingleOrDefaultAsync(x=>x.EventId==eventId,ct);
  if(policy is null)
  {
   policy=new EventRegistrationPolicyModel{EventId=eventId,rbacobject_id=ev.rbacobject_id,created_by=actor,created_at=DateTimeOffset.UtcNow};
   db.Set<EventRegistrationPolicyModel>().Add(policy);
  }
  policy.Capacity=req.Capacity;policy.OpensAt=req.OpensAt?.ToUniversalTime();policy.ClosesAt=req.ClosesAt?.ToUniversalTime();policy.updated_by=actor;policy.updated_at=DateTimeOffset.UtcNow;
  await db.SaveChangesAsync(ct);return policy;
 }
 public async Task<EventParticipantModel?> InviteAsync(Guid eventId,ParticipantAssignmentRequest req,Guid actor,CancellationToken ct)
 {
  var ev=await db.Set<EventModel>().FirstOrDefaultAsync(x=>x.id==eventId,ct);
  if(ev is null||!await CanAsync(actor,ev,"update",ct))return null;
  if((req.UserId.HasValue)==(req.GroupId.HasValue))return null;
  if(req.GroupId is Guid group && !await db.Groups.AnyAsync(x=>x.id==group,ct))return null;
  if(req.UserId is Guid user && !await db.Users.AnyAsync(x=>x.id==user,ct))return null;
  if(await db.Set<EventParticipantModel>().AnyAsync(x=>x.EventId==eventId&&(req.UserId.HasValue?x.UserId==req.UserId:x.GroupId==req.GroupId),ct))return null;
  var now=DateTimeOffset.UtcNow;
  var item=new EventParticipantModel{EventId=eventId,UserId=req.UserId,GroupId=req.GroupId,SourceId=req.Source=="assignment"?EventKnownIds.Assignment:EventKnownIds.Invitation,StatusId=req.Source=="assignment"?EventKnownIds.Accepted:EventKnownIds.Invited,RoleId=req.RoleId??EventKnownIds.Participant,rbacobject_id=ev.rbacobject_id,created_at=now,updated_at=now,created_by=actor,updated_by=actor,StatusChangedAt=now};
  if(!await db.Set<EventParticipantRoleModel>().AnyAsync(x=>x.id==item.RoleId,ct))return null;
  db.Set<EventParticipantModel>().Add(item);await db.SaveChangesAsync(ct);return item;
 }
 public async Task<List<EventParticipantModel>?> ParticipantsAsync(Guid eventId,Guid actor,CancellationToken ct)
 {
  var ev=await db.Set<EventModel>().FirstOrDefaultAsync(x=>x.id==eventId,ct);
  if(ev is null||!await CanAsync(actor,ev,"read",ct))return null;
  return await db.Set<EventParticipantModel>().AsNoTracking().Where(x=>x.EventId==eventId).ToListAsync(ct);
 }
 public Task<List<EventModel>> ListAsync(CancellationToken ct) => db.Set<EventModel>().AsNoTracking().OrderBy(x=>x.StartsAt).ToListAsync(ct);
 public Task<EventModel?> FindAsync(Guid id,CancellationToken ct) => db.Set<EventModel>().AsNoTracking().FirstOrDefaultAsync(x=>x.id==id,ct);
 public Task<List<EventModel>> ChildrenAsync(Guid id,CancellationToken ct) => db.Set<EventModel>().AsNoTracking().Where(x=>x.ParentId==id).OrderBy(x=>x.StartsAt).ToListAsync(ct);
 public Task<List<EventTypeModel>> TypesAsync(CancellationToken ct) => db.Set<EventTypeModel>().AsNoTracking().OrderBy(x=>x.Name).ToListAsync(ct);
 public async Task<bool> CanAsync(Guid actor, EventModel item, string operation, CancellationToken ct) => await security.IsAllowedAsync(actor,item.rbacobject_id,operation,ct);
 public async Task<bool> CreatesCycleAsync(Guid id,Guid? parentId,CancellationToken ct)
 {
  var visited=new HashSet<Guid>{id};
  while(parentId is Guid parent)
  {
   if(!visited.Add(parent)) return true;
   parentId=await db.Set<EventModel>().Where(x=>x.id==parent).Select(x=>x.ParentId).SingleOrDefaultAsync(ct);
  }
  return false;
 }
 public async Task<EventParticipantModel?> RegisterAsync(Guid eventId,Guid userId,CancellationToken ct)
 {
  var item=await db.Set<EventModel>().FirstOrDefaultAsync(x=>x.id==eventId,ct);
  if(item is null || userId==Guid.Empty) return null;
  var policy=await db.Set<EventRegistrationPolicyModel>().SingleOrDefaultAsync(x=>x.EventId==eventId,ct);
  var mode=await ResolveModeAsync(item.EventTypeId,ct);
  if(mode is not ("open" or "approval_required") || policy is null) return null;
  var now=DateTimeOffset.UtcNow;
  if(policy.OpensAt is not null && now<policy.OpensAt || policy.ClosesAt is not null && now>=policy.ClosesAt) return null;
  var participants=db.Set<EventParticipantModel>();
  if(await participants.AnyAsync(x=>x.EventId==eventId && x.UserId==userId,ct)) return null;
  if(policy.Capacity is int capacity && await participants.CountAsync(x=>x.EventId==eventId && (x.StatusId==EventKnownIds.Accepted || x.StatusId==EventKnownIds.Attended),ct)>=capacity) return null;
  var participant=new EventParticipantModel {EventId=eventId,UserId=userId,SourceId=EventKnownIds.SelfRegistration,StatusId=mode=="open"?EventKnownIds.Accepted:EventKnownIds.PendingApproval,RoleId=EventKnownIds.Participant,rbacobject_id=item.rbacobject_id,created_by=userId,updated_by=userId,RespondedAt=now,StatusChangedAt=now};
  participants.Add(participant);
  await db.SaveChangesAsync(ct);
  return participant;
 }
 public async Task<string> ResolveModeAsync(Guid typeId,CancellationToken ct)
 {
  var visited=new HashSet<Guid>();
  while(typeId!=Guid.Empty && visited.Add(typeId))
  {
   var type=await db.Set<EventTypeModel>().AsNoTracking().FirstOrDefaultAsync(x=>x.id==typeId,ct);
   if(type is null) return "disabled";
   if(type.RegistrationMode is "open" or "approval_required" or "disabled") return type.RegistrationMode;
   typeId=type.ParentId??Guid.Empty;
  }
  return "disabled";
 }
 public async Task<bool> RespondAsync(Guid participationId,Guid userId,bool accept,CancellationToken ct)
 {
  var item=await db.Set<EventParticipantModel>().FirstOrDefaultAsync(x=>x.id==participationId && x.UserId==userId,ct);
  if(item is null || item.StatusId!=EventKnownIds.Invited) return false;
  item.StatusId=accept?EventKnownIds.Accepted:EventKnownIds.Declined;
  item.RespondedAt=DateTimeOffset.UtcNow;item.StatusChangedAt=item.RespondedAt;item.updated_at=item.RespondedAt.Value;item.updated_by=userId;
  await db.SaveChangesAsync(ct);return true;
 }
}

public sealed record EventCreateRequest(string Name,string? Description,Guid EventTypeId,Guid? ParentId,DateTimeOffset StartsAt,DateTimeOffset EndsAt,Guid? RbacObjectId);
public sealed record EventUpdateRequest(string Name,string? Description,Guid EventTypeId,Guid? ParentId,DateTimeOffset StartsAt,DateTimeOffset EndsAt);

public sealed record AcademicPlanSegment(Guid? Id,Guid EventTypeId,string Name,DateTimeOffset StartsAt,DateTimeOffset EndsAt);
public sealed record AcademicPlanSaveRequest(DateTimeOffset ExpectedUpdatedAt,List<AcademicPlanSegment> Segments);

public sealed record EventTypeCreateRequest(string Code,string Name,string? Description,string? Abbreviation,string? Color,string RegistrationMode,Guid? ParentId,Guid RbacObjectId);
public sealed record EventTypeUpdateRequest(string Code,string Name,string? Description,string? Abbreviation,string? Color,string RegistrationMode,Guid? ParentId);
public sealed record RegistrationPolicyRequest(int? Capacity,DateTimeOffset? OpensAt,DateTimeOffset? ClosesAt);
public sealed record ParticipantAssignmentRequest(Guid? UserId,Guid? GroupId,string Source,Guid? RoleId);
