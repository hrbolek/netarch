using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
namespace WebAppArchitecture.Events;

/// <summary>
/// RBAC-safe access to event types. Reads are filtered in SQL and paged; writes go through the security layer,
/// which re-checks the operation on the original and the target RBAC context.
/// </summary>
public sealed class EventTypeService(ISecurityLayer securityLayer, IDBLayer unsecuredDb)
 : SecuredBaseService<EventTypeModel>(securityLayer, unsecuredDb)
{
 public const int DefaultPageSize=50;
 public const int MaxPageSize=200;
 private static int PageSize(int? take)=>Math.Clamp(take??DefaultPageSize,1,MaxPageSize);
 private static int Offset(int? skip)=>Math.Max(0,skip??0);

 public Task<List<EventTypeModel>> ListAsync(string? where,int? skip,int? take,CancellationToken ct)
  => EventLookupFilter.Where(SecuredModels(SecurityOperations.Read),where,x=>x.Name+" "+x.Code).OrderBy(x=>x.Name).ThenBy(x=>x.id).Skip(Offset(skip)).Take(PageSize(take)).ToListAsync(ct);

 public Task<EventTypeModel?> FindAsync(Guid id,CancellationToken ct,string operation=SecurityOperations.Read)
  => SecuredModels(operation).Where(x=>x.id==id).FirstOrDefaultAsync(ct);

 public Task<bool> CanAsync(EventTypeModel type,string operation,CancellationToken ct)=>Security.IsAllowedAsync(type.rbacobject_id,operation,ct);

 public Task<bool> ExistsReadableAsync(Guid id,CancellationToken ct)=>SecuredModels(SecurityOperations.Read).AnyAsync(x=>x.id==id,ct);

 public async Task<EventTypeModel?> CreateAsync(EventTypeCreateRequest req,CancellationToken ct)
 {
  if(string.IsNullOrWhiteSpace(req.Code)||string.IsNullOrWhiteSpace(req.Name))return null;
  var scope=req.RbacObjectId;
  if(req.ParentId is Guid parentId)
  {
   var parent=await SecuredModels(SecurityOperations.Create).Where(x=>x.id==parentId).FirstOrDefaultAsync(ct);
   if(parent is null)return null;
   scope=parent.rbacobject_id;
  }
  if(scope==Guid.Empty||!await Security.IsAllowedAsync(scope,SecurityOperations.Create,ct))return null;
  // Uniqueness is a global invariant, so it is checked on the unfiltered source.
  if(await Unsecured_Query().AnyAsync(x=>x.Code==req.Code,ct))return null;
  var actor=Security.CurrentUserId;var now=DateTimeOffset.UtcNow;
  var item=new EventTypeModel{Code=req.Code,Name=req.Name,Description=req.Description,Abbreviation=req.Abbreviation,Color=req.Color,RegistrationMode=req.RegistrationMode,ParentId=req.ParentId,rbacobject_id=scope,created_at=now,updated_at=now,created_by=actor,updated_by=actor};
  return await Security.ExecuteInTransactionAsync(async c=>
  {
   var entity=await Security.StageCreateAsync(item,c);
   await Security.SaveChangesAsync(c);
   return entity;
  },ct);
 }

 public async Task<bool> UpdateAsync(Guid id,EventTypeUpdateRequest req,CancellationToken ct)
 {
  if(string.IsNullOrWhiteSpace(req.Code)||string.IsNullOrWhiteSpace(req.Name)||req.ParentId==id)return false;
  var item=await FindAsync(id,ct,SecurityOperations.Update);
  if(item is null)return false;
  var visited=new HashSet<Guid>{id};var ancestor=req.ParentId;
  while(ancestor is Guid pid)
  {
   if(!visited.Add(pid))return false;
   var parent=await Unsecured_ReadAsync(pid,ct);
   if(parent is null)return false;
   ancestor=parent.ParentId;
  }
  if(await Unsecured_Query().AnyAsync(x=>x.Code==req.Code&&x.id!=id,ct))return false;
  item.Code=req.Code;item.Name=req.Name;item.Description=req.Description;item.Abbreviation=req.Abbreviation;item.Color=req.Color;item.RegistrationMode=req.RegistrationMode;item.ParentId=req.ParentId;item.updated_by=Security.CurrentUserId;item.updated_at=DateTimeOffset.UtcNow;
  await Security.ExecuteInTransactionAsync(async c=>{await Security.StageUpdateAsync(item,c);await Security.SaveChangesAsync(c);},ct);
  return true;
 }

 public async Task<bool> TryDeleteAsync(Guid id,CancellationToken ct)
 {
  if(await FindAsync(id,ct,SecurityOperations.Delete) is null)return false;
  if(await Unsecured_Query().AnyAsync(x=>x.ParentId==id,ct)||await unsecuredDb.Query<EventModel>().AnyAsync(x=>x.EventTypeId==id,ct))return false;
  await DeleteAsync(id,ct);
  return true;
 }

 /// <summary>Resolves the effective registration mode by walking up the type hierarchy (internal invariant, unfiltered).</summary>
 public async Task<string> ResolveModeAsync(Guid typeId,CancellationToken ct)
 {
  var visited=new HashSet<Guid>();
  while(typeId!=Guid.Empty && visited.Add(typeId))
  {
   var type=await Unsecured_ReadAsync(typeId,ct);
   if(type is null) return "disabled";
   if(type.RegistrationMode is "open" or "approval_required" or "disabled") return type.RegistrationMode;
   typeId=type.ParentId??Guid.Empty;
  }
  return "disabled";
 }
}

public sealed record EventTypeCreateRequest(string Code,string Name,string? Description,string? Abbreviation,string? Color,string RegistrationMode,Guid? ParentId,Guid RbacObjectId);
public sealed record EventTypeUpdateRequest(string Code,string Name,string? Description,string? Abbreviation,string? Color,string RegistrationMode,Guid? ParentId);
