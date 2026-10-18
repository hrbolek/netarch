using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Events;

public sealed record EventScopeChoice(Guid Id, string Name);

public sealed record EventFormsModel(
    List<EventTypeModel> Types,
    List<EventScopeChoice> Scopes,
    List<EventModel> Parents,
    EventModel? Current = null);

public static class EventForms
{
    public static async Task<EventFormsModel> LoadAsync(Guid actor, CoreDbContext db, EventService service, EventTypeService typeService, IRbacAuthorizationService security, Guid? currentId, CancellationToken ct)
    {
        var scopes = new List<EventScopeChoice>();
        foreach (var group in await db.Groups.AsNoTracking().ToListAsync(ct))
            if (await security.IsAllowedAsync(actor, group.id, "create", ct))
                scopes.Add(new EventScopeChoice(group.id, group.Name));
        var types = await typeService.ListAsync(null, 0, EventTypeService.MaxPageSize, ct);
        var parents = await service.ListAsync(null, 0, EventService.MaxPageSize, ct);
        var current = currentId is Guid id ? await service.FindAsync(id, ct, "update") : null;
        return new EventFormsModel(types, scopes, parents, current);
    }
}
