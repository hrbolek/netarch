using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.Events;

public sealed record EventsJsonImportResult(int Found, int Inserted, int Existing, int TypesInserted, int MissingType,
    int MissingParent, int Invalid, string[] Issues);

public static class EventsJsonImport
{
    private sealed record SourceEvent(Guid Id, Guid TypeId, Guid? ParentId, string Name,
        string? Description, DateTimeOffset Start, DateTimeOffset End);

    private static readonly string[] StartFields = ["startdate", "starts_at", "startsat", "start_at", "startdatetime", "start_time", "start", "begin"];
    private static readonly string[] EndFields = ["enddate", "ends_at", "endsat", "end_at", "enddatetime", "end_time", "end", "finish"];

    private static bool Property(JsonElement element, out JsonElement value, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject())
                if (names.Any(n => string.Equals(n, property.Name, StringComparison.OrdinalIgnoreCase)))
                { value = property.Value; return true; }
        value = default;
        return false;
    }
    private static string? Value(JsonElement e, params string[] names) =>
        Property(e, out var v, names) && v.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined
            ? v.ToString() : null;
    private static bool GuidValue(JsonElement e, out Guid id, params string[] names) =>
        Guid.TryParse(Value(e, names), out id) && id != Guid.Empty;

    private static bool ParseDate(string? value, out DateTimeOffset date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        // A timestamp with a timezone is unambiguous and retains the source instant.
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out date)
            && (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
                || System.Text.RegularExpressions.Regex.IsMatch(value, @"[+-]\d\d:\d\d$")))
        { date = date.ToUniversalTime(); return true; }
        // Legacy naive datetimes are local civil times in Europe/Prague.
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)) return false;
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague"); }
        catch (TimeZoneNotFoundException) { return false; }
        if (zone.IsInvalidTime(local) || zone.IsAmbiguousTime(local)) return false;
        date = new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
        return true;
    }

    public static async Task<EventsJsonImportResult?> RunAsync(Stream json, Guid actor, Guid scope,
        CoreDbContext db, SecurityService security, CancellationToken ct)
    {
        if (scope == Guid.Empty || !await security.IsAllowedAsync(actor, scope, "create", ct)) return null;
        using var document = await JsonDocument.ParseAsync(json,
            new JsonDocumentOptions { MaxDepth = 64 }, ct);
        if (!Property(document.RootElement, out var events, "events") || events.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Očekávám kořenový JSON objekt s polem 'events'.");
        var found = events.GetArrayLength();
        if (found > 250_000) throw new ArgumentException("Příliš mnoho událostí (limit 250 000).");
        var issues = new List<string>();
        var rows = new Dictionary<Guid, SourceEvent>();
        var invalid = 0;
        var ordinal = 0;
        foreach (var e in events.EnumerateArray())
        {
            ordinal++;
            var type = GuidValue(e, out var typeId, "eventtype_id", "event_type_id", "eventtypeid", "type_id", "typeid");
            var idOk = GuidValue(e, out var id, "id");
            var name = Value(e, "name", "title")?.Trim();
            var starts = ParseDate(Value(e, StartFields), out var start);
            var ends = ParseDate(Value(e, EndFields), out var end);
            var parentRaw = Value(e, "masterevent_id", "parent_id", "parentevent_id", "parentid", "mastereventid");
            var parentOk = string.IsNullOrEmpty(parentRaw) || Guid.TryParse(parentRaw, out _);
            Guid? parent = Guid.TryParse(parentRaw, out var pid) && pid != Guid.Empty ? pid : null;
            if (!idOk || !type || string.IsNullOrWhiteSpace(name) || !starts || !ends || start >= end || !parentOk || id == parent || rows.ContainsKey(id))
            {
                invalid++;
                if (issues.Count < 30) issues.Add($"Záznam #{ordinal}: chybné/duplicitní ID, typ, název, časový interval nebo rodič.");
                continue;
            }
            rows.Add(id, new SourceEvent(id, typeId, parent, name!, Value(e, "description", "description_cs"), start, end));
        }

        // Read only IDs from the database, never materialize another large object graph.
        var types = (await db.Set<EventTypeModel>().AsNoTracking().Select(x => x.id).ToListAsync(ct)).ToHashSet();
        // Preserve the legacy type UUIDs; many of them differ from the separate
        // academic-plan type catalog imported by the admin's other action.
        var sourceTypes = new List<(Guid Id, string Name)>();
        if (Property(document.RootElement, out var sourceTypeArray, "eventtypes") && sourceTypeArray.ValueKind == JsonValueKind.Array)
            foreach (var t in sourceTypeArray.EnumerateArray())
                if (GuidValue(t, out var id, "id") && !string.IsNullOrWhiteSpace(Value(t, "name")))
                    sourceTypes.Add((id, Value(t, "name")!));
        var newTypes = sourceTypes.Where(x => !types.Contains(x.Id)).DistinctBy(x => x.Id).ToList();
        foreach (var t in newTypes) types.Add(t.Id);
        var existing = (await db.Set<EventModel>().AsNoTracking().Select(x => x.id).ToListAsync(ct)).ToHashSet();
        var existingCount = rows.Keys.Count(existing.Contains);
        var missingTypes = rows.Values.Count(x => !existing.Contains(x.Id) && !types.Contains(x.TypeId));
        var pending = rows.Values.Where(x => !existing.Contains(x.Id) && types.Contains(x.TypeId))
            .ToDictionary(x => x.Id);
        var inserted = 0;
        // Insert parents before descendants; a missing parent will never be replaced by null.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var type in newTypes)
            db.Set<EventTypeModel>().Add(new EventTypeModel
            {
                id = type.Id, Code = "legacy-" + type.Id.ToString("N"), Name = type.Name,
                Abbreviation = type.Name.Length <= 12 ? type.Name : null,
                RegistrationMode = "inherit", rbacobject_id = scope,
                created_by = actor, updated_by = actor
            });
        if (newTypes.Count > 0) { await db.SaveChangesAsync(ct); db.ChangeTracker.Clear(); }
        while (pending.Count > 0)
        {
            var ready = pending.Values.Where(x => x.ParentId is null || existing.Contains(x.ParentId.Value))
                .Take(250).ToList();
            if (ready.Count == 0) break;
            var now = DateTimeOffset.UtcNow;
            foreach (var e in ready)
            {
                db.Set<EventModel>().Add(new EventModel
                {
                    id = e.Id, EventTypeId = e.TypeId, ParentId = e.ParentId, Name = e.Name,
                    Description = e.Description, StartsAt = e.Start.ToUniversalTime(), EndsAt = e.End.ToUniversalTime(),
                    rbacobject_id = scope, created_by = actor, updated_by = actor,
                    created_at = now, updated_at = now
                });
                pending.Remove(e.Id);
                existing.Add(e.Id);
                inserted++;
            }
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
        await tx.CommitAsync(ct);
        if (missingTypes > 0) issues.Add($"Přeskočeno {missingTypes} záznamů: odpovídající typ není ani v databázi ani v JSON poli eventtypes.");
        if (pending.Count > 0) issues.Add($"Přeskočeno {pending.Count} záznamů: chybějící rodič nebo cyklus v hierarchii.");
        return new EventsJsonImportResult(found, inserted, existingCount, newTypes.Count, missingTypes, pending.Count, invalid, issues.ToArray());
    }
}
