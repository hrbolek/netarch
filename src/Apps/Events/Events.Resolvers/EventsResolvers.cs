using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Events;

/// <summary>
/// Request handlers of the Events endpoints. Reads go through <see cref="EventService"/>, which is RBAC-filtered
/// and paged by construction; handlers only resolve the actor and shape the HTTP response.
/// </summary>

public static class EventsResolvers
{
    private const int LookupMax = 100;
    private const int TraversalLimit = 5000;
    private static readonly IResult Unauthorized = Results.Unauthorized();

    private static bool WantsHtml(HttpContext http) =>
        http.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

    private static int? LookupTake(int? take) => take.HasValue ? Math.Clamp(take.Value, 1, LookupMax) : null;

    // ---------- UI ----------

    public static async Task<object> TypeListPage(HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, string? where, int? skip, int? take, CancellationToken ct)
    {
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        return await typeSvc.ListAsync(where, skip, LookupTake(take), ct);
    }

    public static async Task<object> EventDetailPage(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, int? skip, int? take, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        if (actor is null) return Unauthorized;
        var item = await svc.FindAsync(id, ct);
        if (item is null) return Results.NotFound();
        var children = await svc.ChildrenAsync(id, skip, take, ct);
        return new EventDetailModel(item, children, await svc.CanAsync(item, "update", ct), await svc.CanAsync(item, "delete", ct));
    }

    public static async Task<object> EventEditPage(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, EventAdministrationService administration, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        if (actor is null) return Unauthorized;
        var item = await svc.FindAsync(id, ct);
        if (item is null) return Results.NotFound();
        if (!await svc.CanAsync(item, "update", ct)) return Results.Forbid();
        return await administration.LoadFormAsync(id, ct);
    }

    public static async Task<object> EventTypeEditPage(Guid id, HttpContext http, ICurrentActorProvider actors, EventTypeService typeSvc, EventAdministrationService administration, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        if (actor is null) return Unauthorized;
        var type = await typeSvc.FindAsync(id, ct);
        if (type is null) return Results.NotFound();
        if (!await typeSvc.CanAsync(type, "update", ct)) return Results.Forbid();
        return new EventTypeEditModel(type, await administration.LoadFormAsync(null, ct));
    }

    /// <summary>Form model used by the new/admin pages.</summary>
    public static async Task<object> EventForm(HttpContext http, ICurrentActorProvider actors, EventAdministrationService administration, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        if (actor is null) return Unauthorized;
        return await administration.LoadFormAsync(null, ct);
    }

    public static async Task<object> EventListPage(HttpContext http, ICurrentActorProvider actors, EventService svc, string? where, int? skip, int? take, CancellationToken ct)
    {
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        return await svc.ListAsync(where, skip, LookupTake(take), ct);
    }

    public static async Task<object> AcademicPlannerEditor(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        if (actor is null) return Unauthorized;
        var year = await svc.FindAsync(id, ct);
        if (year is null) return Results.NotFound();
        var segments = await svc.PlannerSegmentsAsync(id, ct);
        var types = await typeSvc.ListAsync(null, 0, EventTypeService.MaxPageSize, ct);
        return new AcademicPlannerModel(year, segments, types, await svc.CanAsync(year, "update", ct));
    }

    // ---------- API ----------

    public static async Task<IResult> EventListApi(HttpContext http, ICurrentActorProvider actors, EventService svc, string? where, int? skip, int? take, CancellationToken ct)
    {
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        return Results.Ok(await svc.ListAsync(where, skip, LookupTake(take), ct));
    }

    public static async Task<IResult> EventVector(HttpContext http, ICurrentActorProvider actors, EventService svc, int? skip, int? take, CancellationToken ct)
    {
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        return Results.Ok(await svc.ListAsync(null, skip, take, ct));
    }

    public static Task<IResult> EventViewApi(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        View(id, http, actors, svc, false, ct);

    public static Task<IResult> EventViewUi(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        View(id, http, actors, svc, true, ct);

    private static async Task<IResult> View(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, bool ui, CancellationToken ct)
    {
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        var item = await svc.FindAsync(id, ct);
        if (item is null) return Results.NotFound();
        return ui && WantsHtml(http) ? Results.Redirect($"/events/EventResponse/detail/{id}") : Results.Ok(item);
    }

    public static async Task<IResult> EventChildren(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, int? skip, int? take, CancellationToken ct)
    {
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        return Results.Ok(await svc.ChildrenAsync(id, skip, take, ct));
    }

    public static async Task<IResult> EventTypeTree(HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, int? skip, int? take, CancellationToken ct)
    {
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        return Results.Ok(await typeSvc.ListAsync(null, skip, take, ct));
    }

    public static Task<IResult> EventTimetableApi(Guid id, DateOnly? from, DateOnly? to, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, CancellationToken ct) =>
        Timetable(id, from, to, http, actors, svc, typeSvc, false, ct);

    public static Task<IResult> EventTimetableUi(Guid id, DateOnly? from, DateOnly? to, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, CancellationToken ct) =>
        Timetable(id, from, to, http, actors, svc, typeSvc, true, ct);

    private static async Task<IResult> Timetable(Guid id, DateOnly? from, DateOnly? to, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, bool ui, CancellationToken ct)
    {
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        var root = await svc.FindAsync(id, ct);
        if (root is null) return Results.NotFound();
        if (ui && WantsHtml(http)) return EventsTimetableHtmlPage.Render();
        // The requested calendar window is bounded so a timetable does not become
        // an unbounded search API. Dates are interpreted as Prague local dates.
        var zone = PragueTimeZone();
        var first = from ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
        var last = to ?? first.AddDays(6);
        if (last < first || last.DayNumber - first.DayNumber > 30) return Results.BadRequest(new { error = "Date range must cover 1 to 31 days." });
        var start = new DateTimeOffset(first.ToDateTime(TimeOnly.MinValue), zone.GetUtcOffset(first.ToDateTime(TimeOnly.MinValue))).ToUniversalTime();
        var after = last.AddDays(1);
        var end = new DateTimeOffset(after.ToDateTime(TimeOnly.MinValue), zone.GetUtcOffset(after.ToDateTime(TimeOnly.MinValue))).ToUniversalTime();
        var week = TimeSpan.FromDays(7);
        var found = await svc.DescendantsAsync(id, x => !x.IsPlannerSegment && x.StartsAt < end && x.EndsAt > start, TraversalLimit, ct);
        var descendants = found.Where(x => x.EndsAt - x.StartsAt < week).ToList();
        // The requested root can itself be a time-of-day event.
        if (!root.IsPlannerSegment && root.EndsAt - root.StartsAt < week && root.StartsAt < end && root.EndsAt > start) descendants.Insert(0, root);
        var types = await typeSvc.ListAsync(null, 0, EventTypeService.MaxPageSize, ct);
        return Results.Ok(new { root = new { root.id, root.Name }, from = first, to = last, timeZone = "Europe/Prague", events = descendants, types });
    }

    private static TimeZoneInfo PragueTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
        }
    }

    public static Task<IResult> EventAcademicPlanApi(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, CancellationToken ct) =>
        AcademicPlan(id, http, actors, svc, typeSvc, false, ct);

    public static Task<IResult> EventAcademicPlanUi(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, CancellationToken ct) =>
        AcademicPlan(id, http, actors, svc, typeSvc, true, ct);

    private static async Task<IResult> AcademicPlan(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, bool ui, CancellationToken ct)
    {
        if (ui && WantsHtml(http)) return Results.Redirect($"/events/EventResponse/academic-plan/editor/{id}");
        if (actors.TryGetActorUserId(http) is null) return Unauthorized;
        var root = await svc.FindAsync(id, ct);
        if (root is null) return Results.NotFound();
        var descendants = await svc.DescendantsAsync(id, null, TraversalLimit, ct);
        var types = await typeSvc.ListAsync(null, 0, EventTypeService.MaxPageSize, ct);
        return Results.Ok(new { year = root, events = descendants, types });
    }

    public static async Task<IResult> EventAcademicPlanPut(Guid id, AcademicPlanSaveRequest request, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        return await svc.SaveAcademicPlanAsync(id, request, ct) ? Results.NoContent() : Results.Conflict();
    }

    public static async Task<IResult> EventImportJsonPost(HttpRequest request, HttpContext http, ICurrentActorProvider actors, EventAdministrationService administration, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        if (actor is null) return Unauthorized;
        if (!request.HasFormContentType) return Results.BadRequest(new { error = "Očekávám multipart/form-data." });
        var form = await request.ReadFormAsync(ct);
        if (!Guid.TryParse(form["rbacObjectId"], out var scope)) return Results.BadRequest(new { error = "Chybí RBAC rozsah." });
        if (!await administration.CanImportAsync(scope, ct)) return Results.Forbid();
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0 || file.Length > 100L * 1024 * 1024)
            return Results.BadRequest(new { error = "Soubor JSON musí mít 1 až 100 MB." });
        try
        {
            await using var content = file.OpenReadStream();
            var result = await administration.ImportJsonAsync(content, scope, ct);
            return result is null ? Results.Forbid() : Results.Ok(result);
        }
        catch (System.Text.Json.JsonException ex) { return Results.BadRequest(new { error = "Neplatný JSON: " + ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    public static async Task<IResult> EventTypeImportStandardPost(StandardTypeImportRequest req, HttpContext http, ICurrentActorProvider actors, EventAdministrationService administration, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        if (actor is null) return Unauthorized;
        var result = await administration.ImportStandardTypesAsync(req.RbacObjectId, ct);
        return result is null ? Results.Forbid() : Results.Ok(result);
    }

    public static async Task<IResult> EventTypeCreatePost(EventTypeCreateRequest req, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        var item = await typeSvc.CreateAsync(req, ct); return item is null ? Results.Forbid() : Results.Ok(item);
    }

    public static async Task<IResult> EventTypeUpdatePut(Guid id, EventTypeUpdateRequest req, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        return actor is null ? Unauthorized : await typeSvc.UpdateAsync(id, req, ct) ? Results.NoContent() : Results.Conflict();
    }

    public static async Task<IResult> EventTypeDeleteDelete(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, EventTypeService typeSvc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        return actor is null ? Unauthorized : await typeSvc.TryDeleteAsync(id, ct) ? Results.NoContent() : Results.Conflict();
    }

    public static async Task<IResult> EventRegistrationPolicyPut(Guid id, RegistrationPolicyRequest req, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        var policy = await svc.UpsertPolicyAsync(id, req, ct); return policy is null ? Results.Forbid() : Results.Ok(policy);
    }

    public static async Task<IResult> EventParticipants(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        var items = await svc.ParticipantsAsync(id, ct); return items is null ? Results.Forbid() : Results.Ok(items);
    }

    public static async Task<IResult> EventInvitePost(Guid id, ParticipantAssignmentRequest req, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        var item = await svc.InviteAsync(id, req, ct); return item is null ? Results.Forbid() : Results.Ok(item);
    }

    public static async Task<IResult> EventRegisterPost(Guid id, HttpContext http, EventService svc, ICurrentActorProvider actors, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        var result = await svc.RegisterAsync(id, ct);
        return result is null ? Results.Conflict(new { error = "Registration unavailable, duplicate or at capacity." }) : Results.Ok(result);
    }

    public static async Task<IResult> EventParticipantAcceptPost(Guid id, HttpContext http, EventService svc, ICurrentActorProvider actors, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        return actor is null ? Unauthorized : await svc.RespondAsync(id, true, ct) ? Results.Ok() : Results.Conflict();
    }

    public static async Task<IResult> EventParticipantDeclinePost(Guid id, HttpContext http, EventService svc, ICurrentActorProvider actors, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http);
        return actor is null ? Unauthorized : await svc.RespondAsync(id, false, ct) ? Results.Ok() : Results.Conflict();
    }

    public static async Task<IResult> EventCreatePost(EventCreateRequest request, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        var result = await svc.CreateAsync(request, ct);
        return result is null ? Results.Forbid() : Results.Created($"/events/EventResponse/view/{result.id}", result);
    }

    public static async Task<IResult> EventUpdatePut(Guid id, EventUpdateRequest request, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        return await svc.UpdateAsync(id, request, ct) ? Results.NoContent() : Results.Conflict();
    }

    public static async Task<IResult> EventDeleteDelete(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        return await svc.TryDeleteAsync(id, ct) ? Results.NoContent() : Results.Conflict();
    }

    public static async Task<IResult> EventParticipantApprovePost(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        return await svc.PrivilegedStatusAsync(id, EventKnownIds.Accepted, EventKnownIds.PendingApproval, ct) ? Results.NoContent() : Results.Forbid();
    }

    public static async Task<IResult> EventParticipantRecordAttendancePost(Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct)
    {
        var actor = actors.TryGetActorUserId(http); if (actor is null) return Unauthorized;
        return await svc.PrivilegedStatusAsync(id, EventKnownIds.Attended, EventKnownIds.Accepted, ct) ? Results.NoContent() : Results.Forbid();
    }
}
