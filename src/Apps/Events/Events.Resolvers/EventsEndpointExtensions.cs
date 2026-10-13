using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Security;
using Microsoft.AspNetCore.Mvc;

namespace WebAppArchitecture.Events;

/// <summary>HTTP endpoint registration only; retains legacy route contracts and adds OpenAPI operation metadata.</summary>
public static class EventsEndpointExtensions
{
    public static void MapRoutes(IEndpointRouteBuilder endpoints, string prefix, bool ui)
    {
        var r = endpoints.MapGroup("/" + prefix.Trim('/')).WithTags("Events");
        if (ui)
        {
            r.MapGet("/events-assets/{file}", (string file) =>
            {
                if (file != "academic-planner.css" && file != "academic-planner.js") return (IResult)Results.NotFound();
                var asm = typeof(EventsEndpointExtensions).Assembly;
                var name = asm.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("events_assets_" + file.Replace('.', '_'), StringComparison.OrdinalIgnoreCase)
                || x.EndsWith("events_assets." + file, StringComparison.OrdinalIgnoreCase)
                || x.EndsWith("events-assets." + file, StringComparison.OrdinalIgnoreCase));
                if (name is null) return (IResult)Results.NotFound();
                using var stream = asm.GetManifestResourceStream(name);
                if (stream is null) return (IResult)Results.NotFound();
                using var reader = new StreamReader(stream);
                return (IResult)Results.Content(reader.ReadToEnd(), file.EndsWith(".css") ? "text/css" : "text/javascript");
            }).ExcludeFromDescription();
        }

        if (ui)
        {
            r.MapGet("/EventTypeResponse/list", async (HttpContext http, ICurrentActorProvider actors, EventService svc, string? where, int? take, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                var visible = new List<EventTypeModel>();
                foreach (var item in await svc.TypesAsync(ct))
                    if (await svc.CanTypeAsync(actor.Value, item, "read", ct)) visible.Add(item);
                return (object)EventLookupFilter.Apply(visible, where, take, x => x.Name + " " + x.Code);
            }).ApplyViewModel<EventTypeListPage, List<EventTypeModel>>().WithTags("Events").WithName("EventTypeListPage").WithDescription("Lists event types available for read to the acting user; supports JSON and HTML representations for lookup and administration.");
            r.MapGet("/EventResponse/detail/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                var item = await svc.FindAsync(id, ct);
                if (item is null) return (object)Results.NotFound();
                if (!await svc.CanAsync(actor.Value, item, "read", ct)) return (object)Results.Forbid();
                var children = new List<EventModel>();
                foreach (var child in await svc.ChildrenAsync(id, ct))
                    if (await svc.CanAsync(actor.Value, child, "read", ct)) children.Add(child);
                return (object)new EventDetailModel(item, children, await svc.CanAsync(actor.Value, item, "update", ct), await svc.CanAsync(actor.Value, item, "delete", ct));
            }).ApplyViewModel<EventDetailPage, EventDetailModel>().WithTags("Events").WithName("EventDetail").WithDescription("Returns Event resource via /EventResponse/detail/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
            r.MapGet("/EventResponse/edit/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, CoreDbContext db, EventService svc, SecurityService security, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                var item = await svc.FindAsync(id, ct);
                if (item is null) return (object)Results.NotFound();
                if (!await svc.CanAsync(actor.Value, item, "update", ct)) return (object)Results.Forbid();
                return (object)await EventForms.LoadAsync(actor.Value, db, svc, security, id, ct);
            }).ApplyViewModel<EventEditPage, EventFormsModel>().WithTags("Events").WithName("EventEdit").WithDescription("Returns Event resource via /EventResponse/edit/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
            r.MapGet("/EventTypeResponse/edit/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, CoreDbContext db, EventService svc, SecurityService security, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                var type = (await svc.TypesAsync(ct)).FirstOrDefault(x => x.id == id);
                if (type is null) return (object)Results.NotFound();
                if (!await svc.CanTypeAsync(actor.Value, type, "update", ct)) return (object)Results.Forbid();
                return (object)new EventTypeEditModel(type, await EventForms.LoadAsync(actor.Value, db, svc, security, null, ct));
            }).ApplyViewModel<EventTypeEditPage, EventTypeEditModel>().WithTags("Events").WithName("EventTypeEdit").WithDescription("Returns EventType resource via /EventTypeResponse/edit/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        }
        if (ui)
        {
            r.MapGet("/EventResponse/new", async (HttpContext http, ICurrentActorProvider actors, CoreDbContext db, EventService svc, SecurityService security, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                return (object)await EventForms.LoadAsync(actor.Value, db, svc, security, null, ct);
            }).ApplyViewModel<EventCreatePage, EventFormsModel>().WithTags("Events").WithName("EventNew").WithDescription("Returns Event resource via /EventResponse/new. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
            r.MapGet("/EventResponse/admin", async (HttpContext http, ICurrentActorProvider actors, CoreDbContext db, EventService svc, SecurityService security, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                return (object)await EventForms.LoadAsync(actor.Value, db, svc, security, null, ct);
            }).ApplyViewModel<EventAdminPage, EventFormsModel>().WithTags("Events").WithName("EventAdmin").WithDescription("Returns Event resource via /EventResponse/admin. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
            r.MapGet("/EventTypeResponse/new", async (HttpContext http, ICurrentActorProvider actors, CoreDbContext db, EventService svc, SecurityService security, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                return (object)await EventForms.LoadAsync(actor.Value, db, svc, security, null, ct);
            }).ApplyViewModel<EventTypeCreatePage, EventFormsModel>().WithTags("Events").WithName("EventTypeNew").WithDescription("Returns EventType resource via /EventTypeResponse/new. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        }
        if (ui)
            r.MapGet("/EventResponse/list", async (HttpContext http, ICurrentActorProvider actors, EventService svc, string? where, int? take, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                var visible = new List<EventModel>();
                foreach (var item in await svc.ListAsync(ct))
                    if (await svc.CanAsync(actor.Value, item, "read", ct)) visible.Add(item);
                return (object)EventLookupFilter.Apply(visible, where, take, x => x.Name);
            }).ApplyViewModel<EventListPage, List<EventModel>>().WithTags("Events").WithName("EventListPage").WithDescription("Lists events visible to the effective user. JSON returns the event collection; HTML renders standard cards. Read access is enforced for each event.");
        else
            r.MapGet("/EventResponse/list", async (HttpContext http, ICurrentActorProvider actors, EventService svc, string? where, int? take, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
                var visible = new List<EventModel>();
                foreach (var item in await svc.ListAsync(ct)) if (await svc.CanAsync(actor.Value, item, "read", ct)) visible.Add(item);
                return Results.Ok(EventLookupFilter.Apply(visible, where, take, x => x.Name));
            }).WithTags("Events").WithName("EventListApi").WithDescription("Lists events visible to the effective user. JSON returns the event collection; HTML renders standard cards. Read access is enforced for each event.");

        r.MapGet("/EventResponse/vector", async (HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var all = await svc.ListAsync(ct); var visible = new List<EventModel>();
            foreach (var item in all) if (await svc.CanAsync(actor.Value, item, "read", ct)) visible.Add(item);
            return Results.Ok(visible);
        }).WithName("EventVector").WithDescription("Returns Event resource via /EventResponse/vector. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/view/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var item = await svc.FindAsync(id, ct); if (item is null) return Results.NotFound();
            if (!await svc.CanAsync(actor.Value, item, "read", ct)) return Results.Forbid();
            if (ui && http.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
                return Results.Redirect($"/events/EventResponse/detail/{id}");
            return Results.Ok(item);
        }).WithName("EventView").WithDescription("Returns Event resource via /EventResponse/view/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/children/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var visible = new List<EventModel>(); foreach (var item in await svc.ChildrenAsync(id, ct)) if (await svc.CanAsync(actor.Value, item, "read", ct)) visible.Add(item);
            return Results.Ok(visible);
        }).WithName("EventChildren").WithDescription("Returns Event resource via /EventResponse/children/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventTypeResponse/tree", async (HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var visible = new List<EventTypeModel>(); foreach (var item in await svc.TypesAsync(ct)) if (await svc.CanTypeAsync(actor.Value, item, "read", ct)) visible.Add(item);
            return Results.Ok(visible);
        }).WithName("EventTypeTree").WithDescription("Returns EventType resource via /EventTypeResponse/tree. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        // A distinct representation of the same Event tree: time-of-day timetable.
        // The API route remains available in API-only builds; HTML is UI-only.
        r.MapGet("/EventResponse/timetable/{id:guid}", async (Guid id, DateOnly? from, DateOnly? to, HttpContext http, ICurrentActorProvider actors, EventService svc, CoreDbContext db, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var root = await svc.FindAsync(id, ct); if (root is null) return Results.NotFound();
            if (!await svc.CanAsync(actor.Value, root, "read", ct)) return Results.Forbid();
            if (ui && http.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
                return EventsTimetableUI.Render();
            // The requested calendar window is bounded so a timetable does not become
            // an unbounded search API. Dates are interpreted as Prague local dates.
            var zone = EventsTimetableUI.PragueZone();
            var first = from ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
            var last = to ?? first.AddDays(6);
            if (last < first || last.DayNumber - first.DayNumber > 30) return Results.BadRequest(new { error = "Date range must cover 1 to 31 days." });
            var start = new DateTimeOffset(first.ToDateTime(TimeOnly.MinValue), zone.GetUtcOffset(first.ToDateTime(TimeOnly.MinValue))).ToUniversalTime();
            var after = last.AddDays(1);
            var end = new DateTimeOffset(after.ToDateTime(TimeOnly.MinValue), zone.GetUtcOffset(after.ToDateTime(TimeOnly.MinValue))).ToUniversalTime();
            var all = await db.Set<EventModel>().AsNoTracking().ToListAsync(ct);
            var descendants = new List<EventModel>();
            var queue = new Queue<Guid>(); var visited = new HashSet<Guid> { id }; queue.Enqueue(id);
            while (queue.TryDequeue(out var parent))
                foreach (var child in all.Where(e => e.ParentId == parent))
                    if (visited.Add(child.id))
                    {
                        queue.Enqueue(child.id);
                        if (!child.IsPlannerSegment && child.EndsAt - child.StartsAt < TimeSpan.FromDays(7) && child.StartsAt < end && child.EndsAt > start && await svc.CanAsync(actor.Value, child, "read", ct)) descendants.Add(child);
                    }
            // The requested root can itself be a time-of-day event.
            if (!root.IsPlannerSegment && root.EndsAt - root.StartsAt < TimeSpan.FromDays(7) && root.StartsAt < end && root.EndsAt > start) descendants.Insert(0, root);
            var types = new List<EventTypeModel>();
            foreach (var type in await svc.TypesAsync(ct)) if (await svc.CanTypeAsync(actor.Value, type, "read", ct)) types.Add(type);
            return Results.Ok(new { root = new { root.id, root.Name }, from = first, to = last, timeZone = "Europe/Prague", events = descendants, types });
        }).WithName("EventTimetable").WithDescription("Returns Event resource via /EventResponse/timetable/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        if (ui)
        {
            r.MapGet("/EventResponse/academic-plan/editor/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CoreDbContext db, CancellationToken ct) =>
            {
                var actor = actors.TryGetActorUserId(http);
                if (actor is null) return (object)Results.Unauthorized();
                var year = await svc.FindAsync(id, ct);
                if (year is null) return (object)Results.NotFound();
                if (!await svc.CanAsync(actor.Value, year, "read", ct)) return (object)Results.Forbid();
                var items = await db.Set<EventModel>().AsNoTracking().Where(x => x.ParentId == id && x.IsPlannerSegment).ToListAsync(ct);
                var visible = new List<EventModel>();
                foreach (var item in items) if (await svc.CanAsync(actor.Value, item, "read", ct)) visible.Add(item);
                var types = new List<EventTypeModel>();
                foreach (var type in await svc.TypesAsync(ct)) if (await svc.CanTypeAsync(actor.Value, type, "read", ct)) types.Add(type);
                return (object)new AcademicPlannerModel(year, visible, types, await svc.CanAsync(actor.Value, year, "update", ct));
            }).ApplyViewModel<AcademicPlannerPage, AcademicPlannerModel>()
              .WithName("EventAcademicPlannerEditor")
              .WithDescription("Razor academic-year calendar: ISO week half-cells, event-type palette, accessible read-only mode and RBAC-aware save via AcademicPlanPut.");
        }
        r.MapGet("/EventResponse/academic-plan/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CoreDbContext db, CancellationToken ct) =>
        {

            if (ui && http.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
                return Results.Redirect($"/events/EventResponse/academic-plan/editor/{id}");
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var root = await svc.FindAsync(id, ct); if (root is null) return Results.NotFound();
            if (!await svc.CanAsync(actor.Value, root, "read", ct)) return Results.Forbid();
            var all = await db.Set<EventModel>().AsNoTracking().ToListAsync(ct);
            var descendants = new List<EventModel>(); var open = new Queue<Guid>(); var visited = new HashSet<Guid> { id }; open.Enqueue(id);
            while (open.TryDequeue(out var parent)) foreach (var child in all.Where(x => x.ParentId == parent)) if (visited.Add(child.id))
            { open.Enqueue(child.id); if (await svc.CanAsync(actor.Value, child, "read", ct)) descendants.Add(child); }
            var types = new List<EventTypeModel>(); foreach (var type in await svc.TypesAsync(ct)) if (await svc.CanTypeAsync(actor.Value, type, "read", ct)) types.Add(type);
            return Results.Ok(new { year = root, events = descendants, types });
        }).WithName("EventAcademicPlan").WithDescription("Returns Event resource via /EventResponse/academic-plan/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPut("/EventResponse/academic-plan/{id:guid}", async (Guid id, AcademicPlanSaveRequest request, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            return await svc.SaveAcademicPlanAsync(id, request, actor.Value, ct) ? Results.NoContent() : Results.Conflict();
        }).WithName("EventAcademicPlanPut").WithDescription("Updates Event resource via /EventResponse/academic-plan/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventResponse/import-json", async (HttpRequest request, HttpContext http, ICurrentActorProvider actors, CoreDbContext db, SecurityService security, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http);
            if (actor is null) return Results.Unauthorized();
            if (!request.HasFormContentType) return Results.BadRequest(new { error = "Očekávám multipart/form-data." });
            var form = await request.ReadFormAsync(ct);
            if (!Guid.TryParse(form["rbacObjectId"], out var scope)) return Results.BadRequest(new { error = "Chybí RBAC rozsah." });
            if (!await security.IsAllowedAsync(actor.Value, scope, "create", ct)) return Results.Forbid();
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0 || file.Length > 100L * 1024 * 1024)
                return Results.BadRequest(new { error = "Soubor JSON musí mít 1 až 100 MB." });
            try
            {
                await using var content = file.OpenReadStream();
                var result = await EventsJsonImport.RunAsync(content, actor.Value, scope, db, security, ct);
                return result is null ? Results.Forbid() : Results.Ok(result);
            }
            catch (System.Text.Json.JsonException ex) { return Results.BadRequest(new { error = "Neplatný JSON: " + ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).WithTags("Events").DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(105L * 1024 * 1024)).WithName("EventImportJsonPost").WithDescription("Creates or executes Event resource via /EventResponse/import-json. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventTypeResponse/import-standard", async (StandardTypeImportRequest req, HttpContext http, ICurrentActorProvider actors, CoreDbContext db, SecurityService security, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http);
            if (actor is null) return Results.Unauthorized();
            var result = await EventTypeImport.RunAsync(actor.Value, req.RbacObjectId, db, security, ct);
            return result is null ? Results.Forbid() : Results.Ok(result);
        }).WithTags("Events").WithName("EventTypeImportStandardPost").WithDescription("Creates or executes EventType resource via /EventTypeResponse/import-standard. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventTypeResponse/create", async (EventTypeCreateRequest req, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var item = await svc.CreateTypeAsync(req, actor.Value, ct); return item is null ? Results.Forbid() : Results.Ok(item);
        }).WithName("EventTypeCreatePost").ApplyJsonModel<EventTypeModel>().WithDescription("Creates or executes EventType resource via /EventTypeResponse/create. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPut("/EventTypeResponse/update/{id:guid}", async (Guid id, EventTypeUpdateRequest req, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); return actor is null ? Results.Unauthorized() : await svc.UpdateTypeAsync(id, req, actor.Value, ct) ? Results.NoContent() : Results.Conflict();
        }).WithName("EventTypeUpdatePut").WithDescription("Updates EventType resource via /EventTypeResponse/update/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapDelete("/EventTypeResponse/delete/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); return actor is null ? Results.Unauthorized() : await svc.DeleteTypeAsync(id, actor.Value, ct) ? Results.NoContent() : Results.Conflict();
        }).WithName("EventTypeDeleteDelete").WithDescription("Deletes EventType resource via /EventTypeResponse/delete/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPut("/EventResponse/registration-policy/{id:guid}", async (Guid id, RegistrationPolicyRequest req, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var policy = await svc.UpsertPolicyAsync(id, req, actor.Value, ct); return policy is null ? Results.Forbid() : Results.Ok(policy);
        }).WithName("EventRegistrationPolicyPut").WithDescription("Updates Event resource via /EventResponse/registration-policy/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/participants/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var items = await svc.ParticipantsAsync(id, actor.Value, ct); return items is null ? Results.Forbid() : Results.Ok(items);
        }).WithName("EventParticipants").WithDescription("Returns Event resource via /EventResponse/participants/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventResponse/invite/{id:guid}", async (Guid id, ParticipantAssignmentRequest req, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var item = await svc.InviteAsync(id, req, actor.Value, ct); return item is null ? Results.Forbid() : Results.Ok(item);
        }).WithName("EventInvitePost").WithDescription("Creates or executes Event resource via /EventResponse/invite/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventResponse/register/{id:guid}", async (Guid id, HttpContext http, EventService svc, ICurrentActorProvider actors, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var result = await svc.RegisterAsync(id, actor.Value, ct);
            return result is null ? Results.Conflict(new { error = "Registration unavailable, duplicate or at capacity." }) : Results.Ok(result);
        }).WithName("EventRegisterPost").WithDescription("Creates or executes Event resource via /EventResponse/register/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventParticipantResponse/accept/{id:guid}", async (Guid id, HttpContext http, EventService svc, ICurrentActorProvider actors, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http);
            return actor is null ? Results.Unauthorized() : await svc.RespondAsync(id, actor.Value, true, ct) ? Results.Ok() : Results.Conflict();
        }).WithName("EventParticipantAcceptPost").WithDescription("Creates or executes EventParticipant resource via /EventParticipantResponse/accept/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventParticipantResponse/decline/{id:guid}", async (Guid id, HttpContext http, EventService svc, ICurrentActorProvider actors, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http);
            return actor is null ? Results.Unauthorized() : await svc.RespondAsync(id, actor.Value, false, ct) ? Results.Ok() : Results.Conflict();
        }).WithName("EventParticipantDeclinePost").WithDescription("Creates or executes EventParticipant resource via /EventParticipantResponse/decline/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventResponse/create", async (EventCreateRequest request, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            var result = await svc.CreateAsync(request, actor.Value, ct);
            return result is null ? Results.Forbid() : Results.Created($"/events/EventResponse/view/{result.id}", result);
        }).WithName("EventCreatePost").ApplyJsonModel<EventModel>().WithDescription("Creates an event in an authorized scope or under an authorized parent event. Validates event type and time interval; returns created resource and Location.");
        r.MapPut("/EventResponse/update/{id:guid}", async (Guid id, EventUpdateRequest request, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            return await svc.UpdateAsync(id, request, actor.Value, ct) ? Results.NoContent() : Results.Conflict();
        }).WithName("EventUpdatePut").WithDescription("Updates a specific event after RBAC and hierarchy checks. Input times are normalized to UTC in the service; responds 204 or 409.");
        r.MapDelete("/EventResponse/delete/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            return await svc.DeleteAsync(id, actor.Value, ct) ? Results.NoContent() : Results.Conflict();
        }).WithName("EventDeleteDelete").WithDescription("Deletes Event resource via /EventResponse/delete/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventParticipantResponse/approve/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            return await svc.PrivilegedStatusAsync(id, actor.Value, EventKnownIds.Accepted, EventKnownIds.PendingApproval, ct) ? Results.NoContent() : Results.Forbid();
        }).WithName("EventParticipantApprovePost").WithDescription("Creates or executes EventParticipant resource via /EventParticipantResponse/approve/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventParticipantResponse/record-attendance/{id:guid}", async (Guid id, HttpContext http, ICurrentActorProvider actors, EventService svc, CancellationToken ct) =>
        {
            var actor = actors.TryGetActorUserId(http); if (actor is null) return Results.Unauthorized();
            return await svc.PrivilegedStatusAsync(id, actor.Value, EventKnownIds.Attended, EventKnownIds.Accepted, ct) ? Results.NoContent() : Results.Forbid();
        }).WithName("EventParticipantRecordAttendancePost").WithDescription("Creates or executes EventParticipant resource via /EventParticipantResponse/record-attendance/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
    }
}
