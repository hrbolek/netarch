using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormDefinitionService : IBaseService<FormDefinitionModel, FormDefinitionResponse>
{
    Task<FormDefinitionResponse> CreateTypedAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
    Task<FormDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default);
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

            // A form definition is never left without an editable version.
            // Version 1 is created atomically with the definition and shares its RBAC scope.
            var firstVersion = new FormVersionModel(
                Guid.NewGuid(), id, 1, false, null, id,
                now, now, actor, actor);
            await Security.StageCreateAsync(firstVersion, token);

            await Security.SaveChangesAsync(token);
            await securityProjection.SynchronizeRbacObjectAsync(id, token);
            return FormDefinitionResponse.FromModel(created);
        }, ct);
    }

    public async Task<FormDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var definition = await Security.GetByIdAsync<FormDefinitionModel>(id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Form definition not found or not readable.");

        var versions = await Security.Query<FormVersionModel>(SecurityOperations.Read)
            .Where(x => x.form_definition_id == id)
            .OrderByDescending(x => x.version)
            .Select(FormVersionResponse.Projection)
            .ToArrayAsync(ct);

        return new FormDefinitionDetailResponse(FormDefinitionResponse.FromModel(definition), versions);
    }
}
