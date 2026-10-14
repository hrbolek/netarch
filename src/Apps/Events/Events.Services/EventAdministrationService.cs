using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.Events;

/// <summary>
/// Secured orchestration for form choices and scope-authorized bulk imports.
/// The existing import helpers retain their transaction, batching and deduplication behavior.
/// </summary>
public sealed class EventAdministrationService(
    ISecurityLayer securityLayer,
    IDBLayer unsecuredDb,
    CoreDbContext catalogDb,
    SecurityService authorization,
    EventService events,
    EventTypeService types) : SecuredBaseService<EventModel>(securityLayer, unsecuredDb)
{
    public Task<EventFormsModel> LoadFormAsync(Guid? currentId, CancellationToken ct) =>
        EventForms.LoadAsync(Security.CurrentUserId, catalogDb, events, types, authorization, currentId, ct);

    // Keep the pre-upload authorization check ahead of file validation in the resolver.
    public Task<bool> CanImportAsync(Guid scope, CancellationToken ct) =>
        Security.IsAllowedAsync(scope, SecurityOperations.Create, ct);

    public Task<EventsJsonImportResult?> ImportJsonAsync(Stream json, Guid scope, CancellationToken ct) =>
        EventsJsonImport.RunAsync(json, Security.CurrentUserId, scope, catalogDb, authorization, ct);

    public Task<StandardTypeImportResult?> ImportStandardTypesAsync(Guid scope, CancellationToken ct) =>
        EventTypeImport.RunAsync(Security.CurrentUserId, scope, catalogDb, authorization, ct);
}
