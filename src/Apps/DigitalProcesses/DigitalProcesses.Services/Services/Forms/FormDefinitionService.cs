using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormDefinitionService : IBaseService<FormDefinitionModel, FormDefinitionResponse>
{
    Task<FormDefinitionResponse> CreateTypedAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
}

public sealed class FormDefinitionService(ISecurityLayer security, ISecurityProjectionService securityProjection)
    : BaseService<FormDefinitionModel, FormDefinitionResponse>(security), IFormDefinitionService
{
    public async Task<FormDefinitionResponse> CreateTypedAsync(CreateFormDefinitionRequest request, CancellationToken ct = default)
    {
        var (catalog, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, RbacWellKnown.FormDefinitionCreationScopeId, "typed", ct);

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            await CreationScopeGuard.StageOwnedGroupAsync(
                Security, id, $"Form definition: {request.name}", catalog, true, now, actor, token);

            var model = new FormDefinitionModel(
                id, request.name, request.description, id,
                now, now, actor, actor);
            var created = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            await securityProjection.RebuildAllAsync(token);
            return FormDefinitionResponse.FromModel(created);
        }, ct);
    }
}
