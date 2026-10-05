using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormDefinitionService : IBaseService<FormDefinitionModel, FormDefinitionResponse>
{
    Task<FormDefinitionResponse> CreateAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
}
public sealed class FormDefinitionService(ISecurityLayer security) : BaseService<FormDefinitionModel, FormDefinitionResponse>(security), IFormDefinitionService
{
    public Task<FormDefinitionResponse> CreateAsync(CreateFormDefinitionRequest request, CancellationToken ct = default)
        => CreateSecuredAsync(request.rbacobject_id, (id, rbac, now, actor) => new FormDefinitionModel(id, request.name, request.description, rbac, now, now, actor, actor), FormDefinitionResponse.FromModel, ct);

    private async Task<TDto> CreateSecuredAsync<TModel,TDto>(Guid? requestedRbac, Func<Guid,Guid,DateTimeOffset,Guid,TModel> factory, Func<TModel,TDto> map, CancellationToken ct)
        where TModel : class, WebAppArchitecture.Base.Data.ISecuredEntityDBModel
    {
        var rbac = requestedRbac ?? RbacWellKnown.RootSecurityObjectId;
        if (!await Security.IsAllowedAsync(rbac, SecurityOperations.Create, ct)) throw new UnauthorizedAccessException();
        var now = DateTimeOffset.UtcNow; var actor = Security.CurrentUserId;
        return await Security.ExecuteInTransactionAsync(async token => { var m = await Security.StageCreateAsync(factory(Guid.NewGuid(), rbac, now, actor), token); await Security.SaveChangesAsync(token); return map(m); }, ct);
    }
}
