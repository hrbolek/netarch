using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormVersionService : IBaseService<FormVersionModel, FormVersionResponse> { Task<FormVersionResponse> CreateAsync(CreateFormVersionRequest request, CancellationToken ct = default); Task<FormVersionResponse> PublishAsync(Guid id, CancellationToken ct = default); }
public sealed class FormVersionService(ISecurityLayer security) : BaseService<FormVersionModel, FormVersionResponse>(security), IFormVersionService
{
    public async Task<FormVersionResponse> CreateAsync(CreateFormVersionRequest request, CancellationToken ct = default)
    {
        _ = await Security.GetByIdAsync<FormDefinitionModel>(request.form_definition_id, SecurityOperations.Read, ct) ?? throw new KeyNotFoundException("Form definition not found.");
        var rbac=request.rbacobject_id ?? RbacWellKnown.RootSecurityObjectId; if(!await Security.IsAllowedAsync(rbac,SecurityOperations.Create,ct)) throw new UnauthorizedAccessException();
        var now=DateTimeOffset.UtcNow; var actor=Security.CurrentUserId;
        var model=new FormVersionModel(Guid.NewGuid(),request.form_definition_id,request.version,false,null,rbac,now,now,actor,actor);
        var created=await Security.ExecuteInTransactionAsync(async token=>{var x=await Security.StageCreateAsync(model,token);await Security.SaveChangesAsync(token);return FormVersionResponse.FromModel(x);},ct);
        if(request.publish)return await PublishAsync(created.id,ct);
        return created;
    }
    public async Task<FormVersionResponse> PublishAsync(Guid id,CancellationToken ct=default)
    {
        var model=await Security.GetByIdAsync<FormVersionModel>(id,SecurityOperations.Update,ct)??throw new KeyNotFoundException();
        if(model.is_published)return FormVersionResponse.FromModel(model);
        if(!Security.Query<FormNodeModel>(SecurityOperations.Read).Any(x=>x.form_version_id==id))throw new InvalidOperationException("Cannot publish an empty form version.");
        var now=DateTimeOffset.UtcNow;var updated=model with{is_published=true,published_at=now,updated_at=now,updated_by=Security.CurrentUserId};
        return await Security.ExecuteInTransactionAsync(async token=>{var x=await Security.StageUpdateAsync(updated,token);await Security.SaveChangesAsync(token);return FormVersionResponse.FromModel(x);},ct);
    }
}
