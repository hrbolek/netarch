using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormVersionService : IBaseService<FormVersionModel, FormVersionResponse>
{
    Task<FormVersionResponse> CreateContainedAsync(CreateFormVersionRequest request, CancellationToken ct = default);
    Task<FormVersionResponse> PublishAsync(Guid id, CancellationToken ct = default);
}

public sealed class FormVersionService(ISecurityLayer security)
    : BaseService<FormVersionModel, FormVersionResponse>(security), IFormVersionService
{
    public async Task<FormVersionResponse> CreateContainedAsync(CreateFormVersionRequest request, CancellationToken ct = default)
    {
        var (definition, _) = await CreationScopeGuard.RequireEntityAsync<FormDefinitionModel>(
            Security, request.form_definition_id, "contained", ct);

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new FormVersionModel(
            Guid.NewGuid(), definition.id, request.version, false, null,
            definition.rbacobject_id, now, now, actor, actor);

        var created = await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return FormVersionResponse.FromModel(x);
        }, ct);

        return request.publish ? await PublishAsync(created.id, ct) : created;
    }

    public async Task<FormVersionResponse> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var model = await Security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException();
        if (model.is_published) return FormVersionResponse.FromModel(model);
        if (!Security.Query<FormNodeModel>(SecurityOperations.Read).Any(x => x.form_version_id == id))
            throw new InvalidOperationException("Cannot publish an empty form version.");

        var now = DateTimeOffset.UtcNow;
        var updated = model with { is_published = true, published_at = now, updated_at = now, updated_by = Security.CurrentUserId };
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageUpdateAsync(updated, token);
            await Security.SaveChangesAsync(token);
            return FormVersionResponse.FromModel(x);
        }, ct);
    }
}
