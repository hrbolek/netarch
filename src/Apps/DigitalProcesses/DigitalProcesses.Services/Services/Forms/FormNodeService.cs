using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed class FormNodeService(ISecurityLayer security) : BaseService<FormNodeModel, FormNodeResponse>(security), IFormNodeService
{
    public Task<FormNodeResponse> CreateContainedInVersionAsync(CreateFormNodeRequest request, CancellationToken ct = default)
    {
        if (request.parent_id is not null)
            throw new InvalidOperationException("Root form-node creation does not accept parent_id. Use create-in-node.");
        return CreateCoreAsync(request, null, ct);
    }

    public async Task<FormNodeResponse> CreateContainedInNodeAsync(CreateFormNodeRequest request, CancellationToken ct = default)
    {
        if (request.parent_id is not Guid parentId)
            throw new InvalidOperationException("Child form-node creation requires parent_id.");
        return await CreateCoreAsync(request, parentId, ct);
    }

    private async Task<FormNodeResponse> CreateCoreAsync(CreateFormNodeRequest request, Guid? parentId, CancellationToken ct)
    {
        if (request.min_occurs < 0 || request.max_occurs < 1 || request.min_occurs > request.max_occurs)
            throw new ArgumentOutOfRangeException(nameof(request), "Require 0 <= min_occurs <= max_occurs and max_occurs >= 1.");

        var formVersion = await Security.GetByIdAsync<FormVersionModel>(request.form_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Form version not found or not editable.");
        if (formVersion.is_published)
            throw new InvalidOperationException("Published form version is immutable.");

        var type = await Security.GetByIdAsync<FormNodeTypeModel>(request.node_type_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Node type not found.");
        if (request.max_occurs > 1 && !type.can_repeat)
            throw new InvalidOperationException("Selected node type is not repeatable.");

        FormNodeModel? parent = null;
        Guid creationRbac;
        if (parentId is Guid pid)
        {
            (parent, _) = await CreationScopeGuard.RequireEntityAsync<FormNodeModel>(Security, pid, "contained", ct);
            if (parent.form_version_id != request.form_version_id)
                throw new InvalidOperationException("Parent must belong to the same form version.");
            var parentType = await Security.GetByIdAsync<FormNodeTypeModel>(parent.node_type_id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Parent node type not found.");
            if (!parentType.can_have_children)
                throw new InvalidOperationException("Parent node type cannot have children.");
            creationRbac = parent.rbacobject_id;
        }
        else
        {
            var (_, scope) = await CreationScopeGuard.RequireEntityAsync<FormVersionModel>(
                Security, request.form_version_id, "contained", ct);
            creationRbac = scope.RbacObjectId;
        }

        var normalizedKey = request.key?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
            throw new ArgumentException("Key is required.", nameof(request));
        if (Security.Query<FormNodeModel>(SecurityOperations.Read).Any(x =>
                x.form_version_id == request.form_version_id && x.parent_id == parentId && x.key == normalizedKey))
            throw new InvalidOperationException($"Form node key '{normalizedKey}' is already used in this section.");

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var path = parent is null ? MaterializedPath.Root(id) : MaterializedPath.Child(parent.path, id);
        var model = new FormNodeModel(
            id, request.form_version_id, parentId, path, normalizedKey, request.label, request.description,
            request.node_type_id, request.position, request.min_occurs, request.max_occurs,
            request.own_security_scope, request.configuration_json, creationRbac,
            now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return FormNodeResponse.FromModel(x);
        }, ct);
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


    public async Task<FormDesignerResponse> GetDesignerAsync(Guid formVersionId, CancellationToken ct = default)
    {
        var tree = await GetTreeAsync(formVersionId, ct);
        var types = Security.Query<FormNodeTypeModel>(SecurityOperations.Read)
            .OrderBy(x => x.name)
            .Select(FormNodeTypeResponse.Projection)
            .ToArray();

        var typeMap = types.ToDictionary(x => x.id);
        var libraryItems = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id != formVersionId)
            .OrderBy(x => x.label ?? x.key)
            .Select(FormNodeResponse.Projection)
            .ToArray()
            .Where(x => typeMap.TryGetValue(x.node_type_id, out var t) && t.can_have_children)
            .Select(x => new FormLibraryItemResponse(
                x.form_version_id, x.id, x.label ?? x.key, x.key, typeMap[x.node_type_id].renderer_key))
            .ToArray();

        return new FormDesignerResponse(tree.form, tree.nodes, types, libraryItems);
    }

    public async Task DeleteSubtreeAsync(Guid id, CancellationToken ct = default)
    {
        var root = await Security.GetByIdAsync<FormNodeModel>(id, SecurityOperations.Delete, ct)
            ?? throw new KeyNotFoundException("Node not found or deletion denied.");
        var version = await Security.GetByIdAsync<FormVersionModel>(root.form_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Form version not editable.");
        if (version.is_published) throw new InvalidOperationException("Published version is immutable.");
        var descendants = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == root.form_version_id &&
                (x.path == root.path || x.path.StartsWith(root.path + "/")))
            .OrderByDescending(x => x.path.Length).Select(x => x.id).ToArray();
        await Security.ExecuteInTransactionAsync(async token =>
        {
            foreach (var childId in descendants)
            {
                _ = await Security.GetByIdAsync<FormNodeModel>(childId, SecurityOperations.Delete, token)
                    ?? throw new UnauthorizedAccessException("Cannot delete descendant node.");
                await Security.StageDeleteAsync<FormNodeModel>(childId, token);
            }
            await Security.SaveChangesAsync(token);
            return true;
        }, ct);
    }

    public async Task<FormNodeResponse> UpdateDefinitionAsync(Guid id, UpdateFormNodeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.key))
            throw new ArgumentException("Key is required.", nameof(request));
        if (request.min_occurs < 0 || request.max_occurs < 1 || request.min_occurs > request.max_occurs)
            throw new ArgumentOutOfRangeException(nameof(request), "Require 0 <= min_occurs <= max_occurs and max_occurs >= 1.");

        var node = await Security.GetByIdAsync<FormNodeModel>(id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Form node not found or not editable.");
        var version = await Security.GetByIdAsync<FormVersionModel>(node.form_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Form version not found.");
        if (version.is_published)
            throw new InvalidOperationException("Published form version is immutable.");

        var normalizedKey = request.key.Trim();
        var duplicate = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Any(x => x.form_version_id == node.form_version_id && x.parent_id == node.parent_id && x.id != id && x.key == normalizedKey);
        if (duplicate)
            throw new InvalidOperationException($"Form node key '{request.key}' is already used in this section.");

        var now = DateTimeOffset.UtcNow;
        var updated = node with
        {
            key = request.key.Trim(),
            label = request.label,
            description = request.description,
            configuration_json = request.configuration_json,
            min_occurs = request.min_occurs,
            max_occurs = request.max_occurs,
            updated_at = now,
            updated_by = Security.CurrentUserId
        };
        await Security.StageUpdateAsync(updated, ct);
        await Security.SaveChangesAsync(ct);
        return FormNodeResponse.FromModel(updated);
    }

    public async Task<IReadOnlyCollection<FormNodeResponse>> ImportSubtreeAsync(ImportFormSubtreeRequest request, CancellationToken ct = default)
    {
        var targetVersion = await Security.GetByIdAsync<FormVersionModel>(request.target_form_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Target form version not found or not editable.");
        if (targetVersion.is_published)
            throw new InvalidOperationException("Published form version is immutable.");

        var sourceRoot = await Security.GetByIdAsync<FormNodeModel>(request.source_node_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Library source node not found.");
        var sourceNodes = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == sourceRoot.form_version_id && (x.path == sourceRoot.path || x.path.StartsWith(sourceRoot.path + "/")))
            .OrderBy(x => x.path)
            .ThenBy(x => x.position)
            .ToArray();

        FormNodeModel? targetParent = null;
        Guid targetRbac;
        string? targetParentPath = null;
        if (request.target_parent_id is Guid parentId)
        {
            targetParent = await Security.GetByIdAsync<FormNodeModel>(parentId, SecurityOperations.Update, ct)
                ?? throw new KeyNotFoundException("Target parent not found or not editable.");
            if (targetParent.form_version_id != request.target_form_version_id)
                throw new InvalidOperationException("Target parent belongs to another form version.");
            var parentType = await Security.GetByIdAsync<FormNodeTypeModel>(targetParent.node_type_id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Target parent node type not found.");
            if (!parentType.can_have_children)
                throw new InvalidOperationException("Target parent cannot contain children.");
            targetRbac = targetParent.rbacobject_id;
            targetParentPath = targetParent.path;
        }
        else
        {
            targetRbac = targetVersion.rbacobject_id;
        }

        var existingKeys = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == request.target_form_version_id && x.parent_id == request.target_parent_id)
            .Select(x => x.key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var prefix = string.IsNullOrWhiteSpace(request.key_prefix) ? null : request.key_prefix!.Trim().Trim('.');
        string NewKey(string sourceKey) => prefix is null ? sourceKey : $"{prefix}.{sourceKey}";
        // Only the imported root is a sibling of existing nodes in the target section.
        // Child keys are scoped to their freshly created parent nodes.
        if (existingKeys.Contains(NewKey(sourceRoot.key)))
            throw new InvalidOperationException($"Import would create duplicate key '{NewKey(sourceRoot.key)}' in the target section. Use a key prefix.");

        var idMap = sourceNodes.ToDictionary(x => x.id, _ => Guid.NewGuid());
        var pathMap = new Dictionary<Guid, string>();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var created = new List<FormNodeModel>(sourceNodes.Length);

        foreach (var source in sourceNodes)
        {
            var newId = idMap[source.id];
            var isRoot = source.id == sourceRoot.id;
            Guid? newParentId;
            string newPath;
            if (isRoot)
            {
                newParentId = request.target_parent_id;
                newPath = targetParentPath is null ? MaterializedPath.Root(newId) : MaterializedPath.Child(targetParentPath, newId);
            }
            else
            {
                if (source.parent_id is not Guid oldParent || !idMap.TryGetValue(oldParent, out var mappedParent))
                    throw new InvalidOperationException("Source subtree is structurally inconsistent.");
                newParentId = mappedParent;
                newPath = MaterializedPath.Child(pathMap[oldParent], newId);
            }
            pathMap[source.id] = newPath;
            created.Add(source with
            {
                id = newId,
                form_version_id = request.target_form_version_id,
                parent_id = newParentId,
                path = newPath,
                key = NewKey(source.key),
                position = isRoot ? request.position : source.position,
                // Library import is a structural clone. Security is inherited from the target container.
                own_security_scope = false,
                rbacobject_id = targetRbac,
                created_at = now,
                updated_at = now,
                created_by = actor,
                updated_by = actor
            });
        }

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            foreach (var model in created)
                await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return (IReadOnlyCollection<FormNodeResponse>)created.Select(FormNodeResponse.FromModel).ToArray();
        }, ct);
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
        if (Security.Query<FormNodeModel>(SecurityOperations.Read).Any(x =>
                x.form_version_id == node.form_version_id && x.parent_id == request.parent_id &&
                x.id != id && x.key == node.key))
            throw new InvalidOperationException($"Form node key '{node.key}' is already used in the destination section.");
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
