using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormNodeTypeService : IBaseService<FormNodeTypeModel, FormNodeTypeResponse> { Task<FormNodeTypeResponse> CreateAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default); }
public sealed class FormNodeTypeService(ISecurityLayer security) : BaseService<FormNodeTypeModel, FormNodeTypeResponse>(security), IFormNodeTypeService
{
    public async Task<FormNodeTypeResponse> CreateAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default)
    {
        if(request.parent_type_id is Guid p) _=await Security.GetByIdAsync<FormNodeTypeModel>(p,SecurityOperations.Read,ct) ?? throw new KeyNotFoundException("Parent node type not found.");
        var rbac=request.rbacobject_id ?? RbacWellKnown.RootSecurityObjectId; if(!await Security.IsAllowedAsync(rbac,SecurityOperations.Create,ct)) throw new UnauthorizedAccessException();
        var now=DateTimeOffset.UtcNow;var actor=Security.CurrentUserId;
        var model=new FormNodeTypeModel(Guid.NewGuid(),request.parent_type_id,request.name,request.renderer_key,request.can_have_children,request.can_have_value,request.can_repeat,request.configuration_schema_json,rbac,now,now,actor,actor);
        return await Security.ExecuteInTransactionAsync(async token=>{var x=await Security.StageCreateAsync(model,token);await Security.SaveChangesAsync(token);return FormNodeTypeResponse.FromModel(x);},ct);
    }
}
