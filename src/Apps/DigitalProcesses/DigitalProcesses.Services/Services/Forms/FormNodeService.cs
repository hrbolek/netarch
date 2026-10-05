using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormNodeService : IBaseService<FormNodeModel, FormNodeResponse>
{
    Task<FormNodeResponse> CreateNodeAsync(CreateFormNodeRequest request, CancellationToken ct = default);
    Task<FormNodeResponse> MoveAsync(Guid id, MoveFormNodeRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<FormNodeResponse>> GetSubtreeAsync(Guid id, CancellationToken ct = default);
    Task<FormTreeResponse> GetTreeAsync(Guid formVersionId, CancellationToken ct = default);
}
public sealed class FormNodeService(ISecurityLayer security) : BaseService<FormNodeModel, FormNodeResponse>(security), IFormNodeService
{
    public async Task<FormNodeResponse> CreateNodeAsync(CreateFormNodeRequest request, CancellationToken ct = default)
    {
        if(request.min_occurs<0 || request.max_occurs<1 || request.min_occurs>request.max_occurs) throw new ArgumentOutOfRangeException(nameof(request),"Require 0 <= min_occurs <= max_occurs and max_occurs >= 1.");
        var formVersion=await Security.GetByIdAsync<FormVersionModel>(request.form_version_id,SecurityOperations.Update,ct) ?? throw new KeyNotFoundException("Form version not found or not editable.");
        if(formVersion.is_published) throw new InvalidOperationException("Published form version is immutable.");
        var type=await Security.GetByIdAsync<FormNodeTypeModel>(request.node_type_id,SecurityOperations.Read,ct) ?? throw new KeyNotFoundException("Node type not found.");
        if(request.max_occurs>1 && !type.can_repeat) throw new InvalidOperationException("Selected node type is not repeatable.");
        FormNodeModel? parent=null;
        if(request.parent_id is Guid parentId)
        {
            parent=await Security.GetByIdAsync<FormNodeModel>(parentId,SecurityOperations.Update,ct) ?? throw new KeyNotFoundException("Parent node not found or not editable.");
            if(parent.form_version_id!=request.form_version_id) throw new InvalidOperationException("Parent must belong to the same form version.");
            var parentType=await Security.GetByIdAsync<FormNodeTypeModel>(parent.node_type_id,SecurityOperations.Read,ct) ?? throw new KeyNotFoundException("Parent node type not found.");
            if(!parentType.can_have_children) throw new InvalidOperationException("Parent node type cannot have children.");
        }
        var id=Guid.NewGuid(); var rbac=request.rbacobject_id ?? parent?.rbacobject_id ?? RbacWellKnown.RootSecurityObjectId;
        if(!await Security.IsAllowedAsync(rbac,SecurityOperations.Create,ct)) throw new UnauthorizedAccessException();
        var now=DateTimeOffset.UtcNow; var actor=Security.CurrentUserId; var path=parent is null?MaterializedPath.Root(id):MaterializedPath.Child(parent.path,id);
        var model=new FormNodeModel(id,request.form_version_id,request.parent_id,path,request.key,request.label,request.description,request.node_type_id,request.position,request.min_occurs,request.max_occurs,request.own_security_scope,request.configuration_json,rbac,now,now,actor,actor);
        return await Security.ExecuteInTransactionAsync(async token=>{var x=await Security.StageCreateAsync(model,token);await Security.SaveChangesAsync(token);return FormNodeResponse.FromModel(x);},ct);
    }

    public Task<IReadOnlyCollection<FormNodeResponse>> GetSubtreeAsync(Guid id, CancellationToken ct = default)
    {
        var root=Security.Query<FormNodeModel>(SecurityOperations.Read).SingleOrDefault(x=>x.id==id) ?? throw new KeyNotFoundException("Form node not found.");
        IReadOnlyCollection<FormNodeResponse> result=Security.Query<FormNodeModel>(SecurityOperations.Read).Where(x=>x.form_version_id==root.form_version_id && (x.path==root.path || x.path.StartsWith(root.path+"/"))).OrderBy(x=>x.path).ThenBy(x=>x.position).Select(FormNodeResponse.Projection).ToArray();
        return Task.FromResult(result);
    }

    public async Task<FormTreeResponse> GetTreeAsync(Guid formVersionId, CancellationToken ct = default)
    {
        var form=await Security.GetByIdAsync<FormVersionModel>(formVersionId,SecurityOperations.Read,ct) ?? throw new KeyNotFoundException("Form version not found.");
        var nodes=Security.Query<FormNodeModel>(SecurityOperations.Read).Where(x=>x.form_version_id==formVersionId).OrderBy(x=>x.path).ThenBy(x=>x.position).Select(FormNodeResponse.Projection).ToArray();
        return new(FormVersionResponse.FromModel(form),nodes);
    }

    public async Task<FormNodeResponse> MoveAsync(Guid id, MoveFormNodeRequest request, CancellationToken ct = default)
    {
        var node=await Security.GetByIdAsync<FormNodeModel>(id,SecurityOperations.Update,ct) ?? throw new KeyNotFoundException("Form node not found or not editable.");
        var version=await Security.GetByIdAsync<FormVersionModel>(node.form_version_id,SecurityOperations.Update,ct) ?? throw new KeyNotFoundException("Form version not found.");
        if(version.is_published) throw new InvalidOperationException("Published form version is immutable.");
        FormNodeModel? parent=null;
        if(request.parent_id is Guid pid)
        {
            parent=await Security.GetByIdAsync<FormNodeModel>(pid,SecurityOperations.Update,ct) ?? throw new KeyNotFoundException("New parent not found or not editable.");
            if(parent.form_version_id!=node.form_version_id) throw new InvalidOperationException("Cannot move between form versions.");
            if(MaterializedPath.IsDescendantOrSelf(parent.path,node.path)) throw new InvalidOperationException("Cannot move a node below itself or its descendant.");
        }
        var oldPath=node.path; var newPath=parent is null?MaterializedPath.Root(node.id):MaterializedPath.Child(parent.path,node.id); var now=DateTimeOffset.UtcNow; var actor=Security.CurrentUserId;
        return await Security.ExecuteInTransactionAsync(async token=>
        {
            var subtree=Security.Query<FormNodeModel>(SecurityOperations.Update).Where(x=>x.form_version_id==node.form_version_id && (x.path==oldPath || x.path.StartsWith(oldPath+"/"))).OrderBy(x=>x.path).ToArray();
            foreach(var current in subtree)
            {
                var suffix=current.path[oldPath.Length..];
                var updated=current with { parent_id=current.id==node.id?request.parent_id:current.parent_id, path=newPath+suffix, position=current.id==node.id?request.position:current.position, updated_at=now, updated_by=actor };
                await Security.StageUpdateAsync(updated,token);
            }
            await Security.SaveChangesAsync(token);
            var moved=subtree.Single(x=>x.id==node.id) with { parent_id=request.parent_id,path=newPath,position=request.position,updated_at=now,updated_by=actor };
            return FormNodeResponse.FromModel(moved);
        },ct);
    }
}
