using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.Events;

public sealed record EventScopeChoice(Guid Id, string Name);
public sealed record EventFormsModel(
    List<EventTypeModel> Types,
    List<EventScopeChoice> Scopes,
    List<EventModel> Parents,
    EventModel? Current = null);

public static class EventForms
{
    public static async Task<EventFormsModel> LoadAsync(Guid actor, CoreDbContext db, EventService service, SecurityService security, Guid? currentId, CancellationToken ct)
    {
        var scopes = new List<EventScopeChoice>();
        foreach (var group in await db.Groups.AsNoTracking().ToListAsync(ct))
            if (await security.IsAllowedAsync(actor, group.id, "create", ct))
                scopes.Add(new EventScopeChoice(group.id, group.Name));
        var types = new List<EventTypeModel>();
        foreach (var type in await service.TypesAsync(ct))
            if (await service.CanTypeAsync(actor, type, "read", ct)) types.Add(type);
        var parents = new List<EventModel>();
        foreach (var ev in await service.ListAsync(ct))
            if (await service.CanAsync(actor, ev, "read", ct)) parents.Add(ev);
        EventModel? current = null;
        if (currentId is Guid id)
        {
            current = await service.FindAsync(id, ct);
            if (current is null || !await service.CanAsync(actor, current, "update", ct)) current = null;
        }
        return new EventFormsModel(types, scopes, parents, current);
    }
}
