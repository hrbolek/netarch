using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed class FormNodeTypeService(ISecurityLayer security, ISecurityProjectionService securityProjection)
    : BaseService<FormNodeTypeModel, FormNodeTypeResponse>(security), IFormNodeTypeService
{
    public async Task<FormNodeTypeResponse> CreateTypedAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default)
    {
        if (request.parent_type_id is not null)
            throw new InvalidOperationException("Typed root creation does not accept parent_type_id. Use create-contained.");

        var (catalog, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, RbacWellKnown.FormNodeTypeCreationScopeId, "typed", ct);
        return await CreateCoreAsync(request, catalog, null, ct);
    }

    public async Task<FormNodeTypeResponse> CreateContainedAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default)
    {
        if (request.parent_type_id is not Guid parentTypeId)
            throw new InvalidOperationException("Contained FormNodeType creation requires parent_type_id.");

        var (parentType, _) = await CreationScopeGuard.RequireEntityAsync<FormNodeTypeModel>(
            Security, parentTypeId, "contained", ct);
        var parentGroup = Security.Query<GroupModel>(SecurityOperations.Create)
            .SingleOrDefault(x => x.id == parentType.rbacobject_id)
            ?? throw new InvalidOperationException("Parent FormNodeType security scope is not a Group.");

        return await CreateCoreAsync(request, parentGroup, parentType.id, ct);
    }

    private async Task<FormNodeTypeResponse> CreateCoreAsync(
        CreateFormNodeTypeRequest request,
        GroupModel parentGroup,
        Guid? parentTypeId,
        CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            await CreationScopeGuard.StageOwnedGroupAsync(
                Security, id, $"Form node type: {request.name}", parentGroup, true, now, actor, token);
            var model = new FormNodeTypeModel(
                id, parentTypeId, request.name, request.renderer_key,
                request.can_have_children, request.can_have_value, request.can_repeat,
                request.configuration_schema_json, id, now, now, actor, actor);
            var created = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            await securityProjection.SynchronizeRbacObjectAsync(id, token);
            return FormNodeTypeResponse.FromModel(created);
        }, ct);
    }
}
