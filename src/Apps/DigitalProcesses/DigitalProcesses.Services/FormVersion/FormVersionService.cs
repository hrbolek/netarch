using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormVersion;
/// <summary>
/// Služba Form Version Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed partial class FormVersionService(ISecurityLayer security, IDBLayer unsecuredDb, RtfTemplateStore store, IFormDocumentService formDocuments)
    : SecuredBaseService<FormVersionModel>(security, unsecuredDb), IFormVersionService
{
    public async Task<FormTemplatesPageData?> TemplatePageAsync(Guid id, CancellationToken ct = default)
    {
        var version = await Security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Read, ct);
        if (version is null) return null;
        var mayEdit = !version.is_published &&
            await Security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Update, ct) is not null;
        return new(FormVersionResponse.FromModel(version) with { templates = store.List(id) }, mayEdit);
    }

    public async Task<FormVersionResponse> CreateNextAsync(Guid definitionId, CancellationToken ct = default)
    {
        _ = await Security.GetByIdAsync<FormDefinitionModel>(definitionId, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Form definition not found or not readable.");
        var next = Security.Query<FormVersionModel>(SecurityOperations.Read)
            .Where(v => v.form_definition_id == definitionId).Select(v => v.version).ToArray().DefaultIfEmpty(0).Max() + 1;
        return await CreateContainedAsync(new CreateFormVersionRequest(definitionId, next), ct);
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CurrentUserId, ExecuteInTransactionAsync, StageCreateAsync, Query; pořadí a parametry viz implementace.
    /// </summary>
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
            var source = Security.Query<FormVersionModel>(SecurityOperations.Read)
                .Where(v => v.form_definition_id == definition.id && v.version < request.version)
                .OrderByDescending(v => v.version).FirstOrDefault();
            if (source is not null)
            {
                var nodes = Security.Query<FormNodeModel>(SecurityOperations.Read)
                    .Where(n => n.form_version_id == source.id).ToArray();
                var ids = nodes.ToDictionary(n => n.id, _ => Guid.NewGuid());
                foreach (var node in nodes.OrderBy(n => n.path.Length))
                {
                    var newId = ids[node.id];
                    var mappedPath = node.path;
                    foreach (var pair in ids)
                        mappedPath = mappedPath.Replace(pair.Key.ToString("N"), pair.Value.ToString("N"), StringComparison.OrdinalIgnoreCase);
                    var clone = node with {
                        id = newId, form_version_id = model.id,
                        parent_id = node.parent_id is Guid parent ? ids[parent] : null,
                        path = mappedPath, rbacobject_id = definition.rbacobject_id,
                        created_at = now, updated_at = now, created_by = actor, updated_by = actor
                    };
                    await Security.StageCreateAsync(clone, token);
                }
            }
            await Security.SaveChangesAsync(token);
            return FormVersionResponse.FromModel(x);
        }, ct);

        return request.publish ? await PublishAsync(created.id, ct) : created;
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: GetByIdAsync, Query, ExecuteInTransactionAsync, StageDeleteAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task DeleteDraftAsync(Guid id, CancellationToken ct = default)
    {
        var version = await Security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Delete, ct)
            ?? throw new KeyNotFoundException("Form version not found or not deletable.");
        if (version.is_published) throw new InvalidOperationException("Published versions cannot be deleted.");
        if (Security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read).Any(p => p.form_version_id == id) ||
            Security.Query<SubmissionModel>(SecurityOperations.Read).Any(p => p.form_version_id == id))
            throw new InvalidOperationException("Form version is referenced by a process or submission.");
        var nodes = Security.Query<FormNodeModel>(SecurityOperations.Read).Where(n => n.form_version_id == id).ToArray();
        var nodeIds = nodes.Select(n => n.id).ToArray();
        if (Security.Query<WorkflowStateAccessRuleModel>(SecurityOperations.Read)
            .Any(r => r.target_form_node_id != null && nodeIds.Contains(r.target_form_node_id.Value)))
            throw new InvalidOperationException("Form nodes are referenced by workflow access rules.");
        await Security.ExecuteInTransactionAsync(async token => {
            foreach (var n in nodes.OrderByDescending(n => n.path.Length))
                await Security.StageDeleteAsync<FormNodeModel>(n.id, token);
            await Security.StageDeleteAsync<FormVersionModel>(id, token);
            await Security.SaveChangesAsync(token);
        }, ct);
    }

    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: GetByIdAsync, Query, CurrentUserId, ExecuteInTransactionAsync, StageUpdateAsync, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
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
