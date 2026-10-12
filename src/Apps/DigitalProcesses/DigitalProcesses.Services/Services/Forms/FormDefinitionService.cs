using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba Form Definition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class FormDefinitionService(ISecurityLayer security, ISecurityProjectionService securityProjection, IDBLayer unsecuredDb)
    : SecuredBaseService<FormDefinitionModel>(security, unsecuredDb), IFormDefinitionService
{
    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CurrentUserId, ExecuteInTransactionAsync; pořadí a parametry viz implementace.
    /// </summary>
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

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: GetByIdAsync, Query; pořadí a parametry viz implementace.
    /// </summary>
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
