using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Events;

/// <summary>
/// Secured orchestration for form choices and scope-authorized bulk imports.
/// Import helpers retain their transaction, batching and deduplication behavior and independently
/// check Create against the supplied scope before any import data is read or written.
/// </summary>

public sealed class EventAdministrationService(
    IAuthorizationLayer securityLayer,
    IDBLayer unsecuredDb,
    CoreDbContext catalogDb,
    IRbacAuthorizationService authorization,
    EventService events,
    EventTypeService types) : SecuredEntityService<EventModel>(securityLayer, unsecuredDb)
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
