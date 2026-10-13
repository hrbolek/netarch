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
    /// <summary>JSON-only endpoints (no Razor pages or UI assets).</summary>
    public static void MapApi(IEndpointRouteBuilder endpoints, string prefix) =>
        MapApiRoutes(Group(endpoints, prefix), htmlNegotiation: false);

    /// <summary>Razor pages and UI assets together with the API routes they build on.</summary>
    public static void MapUi(IEndpointRouteBuilder endpoints, string prefix)
    {
        var r = Group(endpoints, prefix);
        MapUiRoutes(r);
        MapApiRoutes(r, htmlNegotiation: true);
    }

    private static RouteGroupBuilder Group(IEndpointRouteBuilder endpoints, string prefix) =>
        endpoints.MapGroup("/" + prefix.Trim('/')).WithTags("Events");

    private static void MapUiRoutes(RouteGroupBuilder r)
    {
        r.MapGet("/events-assets/{file}", EventsResolvers.Asset).ExcludeFromDescription();
        r.MapGet("/EventTypeResponse/list", EventsResolvers.TypeListPage).ApplyViewModel<EventTypeListPage, List<EventTypeModel>>().WithTags("Events").WithName("EventTypeListPage").WithDescription("Lists event types available for read to the acting user; supports JSON and HTML representations for lookup and administration.");
        r.MapGet("/EventResponse/detail/{id:guid}", EventsResolvers.EventDetailPage).ApplyViewModel<EventDetailPage, EventDetailModel>().WithTags("Events").WithName("EventDetail").WithDescription("Returns Event resource via /EventResponse/detail/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/edit/{id:guid}", EventsResolvers.EventEditPage).ApplyViewModel<EventEditPage, EventFormsModel>().WithTags("Events").WithName("EventEdit").WithDescription("Returns Event resource via /EventResponse/edit/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventTypeResponse/edit/{id:guid}", EventsResolvers.EventTypeEditPage).ApplyViewModel<EventTypeEditPage, EventTypeEditModel>().WithTags("Events").WithName("EventTypeEdit").WithDescription("Returns EventType resource via /EventTypeResponse/edit/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/new", EventsResolvers.EventForm).ApplyViewModel<EventCreatePage, EventFormsModel>().WithTags("Events").WithName("EventNew").WithDescription("Returns Event resource via /EventResponse/new. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/admin", EventsResolvers.EventForm).ApplyViewModel<EventAdminPage, EventFormsModel>().WithTags("Events").WithName("EventAdmin").WithDescription("Returns Event resource via /EventResponse/admin. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventTypeResponse/new", EventsResolvers.EventForm).ApplyViewModel<EventTypeCreatePage, EventFormsModel>().WithTags("Events").WithName("EventTypeNew").WithDescription("Returns EventType resource via /EventTypeResponse/new. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/list", EventsResolvers.EventListPage).ApplyViewModel<EventListPage, List<EventModel>>().WithTags("Events").WithName("EventListPage").WithDescription("Lists events visible to the effective user. JSON returns the event collection; HTML renders standard cards. Read access is enforced for each event.");
        r.MapGet("/EventResponse/academic-plan/editor/{id:guid}", EventsResolvers.AcademicPlannerEditor).ApplyViewModel<AcademicPlannerPage, AcademicPlannerModel>()
          .WithName("EventAcademicPlannerEditor")
          .WithDescription("Razor academic-year calendar: ISO week half-cells, event-type palette, accessible read-only mode and RBAC-aware save via AcademicPlanPut.");
    }

    private static void MapApiRoutes(RouteGroupBuilder r, bool htmlNegotiation)
    {
        if (!htmlNegotiation)
            r.MapGet("/EventResponse/list", EventsResolvers.EventListApi).WithTags("Events").WithName("EventListApi").WithDescription("Lists events visible to the effective user. JSON returns the event collection; HTML renders standard cards. Read access is enforced for each event.");
        r.MapGet("/EventResponse/vector", EventsResolvers.EventVector).WithName("EventVector").WithDescription("Returns Event resource via /EventResponse/vector. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/view/{id:guid}", htmlNegotiation ? (Delegate)EventsResolvers.EventViewUi : EventsResolvers.EventViewApi).WithName("EventView").WithDescription("Returns Event resource via /EventResponse/view/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/children/{id:guid}", EventsResolvers.EventChildren).WithName("EventChildren").WithDescription("Returns Event resource via /EventResponse/children/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventTypeResponse/tree", EventsResolvers.EventTypeTree).WithName("EventTypeTree").WithDescription("Returns EventType resource via /EventTypeResponse/tree. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        // A distinct representation of the same Event tree: time-of-day timetable.
        // The API route remains available in API-only builds; HTML is UI-only.
        r.MapGet("/EventResponse/timetable/{id:guid}", htmlNegotiation ? (Delegate)EventsResolvers.EventTimetableUi : EventsResolvers.EventTimetableApi).WithName("EventTimetable").WithDescription("Returns Event resource via /EventResponse/timetable/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/academic-plan/{id:guid}", htmlNegotiation ? (Delegate)EventsResolvers.EventAcademicPlanUi : EventsResolvers.EventAcademicPlanApi).WithName("EventAcademicPlan").WithDescription("Returns Event resource via /EventResponse/academic-plan/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPut("/EventResponse/academic-plan/{id:guid}", EventsResolvers.EventAcademicPlanPut).WithName("EventAcademicPlanPut").WithDescription("Updates Event resource via /EventResponse/academic-plan/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventResponse/import-json", EventsResolvers.EventImportJsonPost).WithTags("Events").DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(105L * 1024 * 1024)).WithName("EventImportJsonPost").WithDescription("Creates or executes Event resource via /EventResponse/import-json. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventTypeResponse/import-standard", EventsResolvers.EventTypeImportStandardPost).WithTags("Events").WithName("EventTypeImportStandardPost").WithDescription("Creates or executes EventType resource via /EventTypeResponse/import-standard. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventTypeResponse/create", EventsResolvers.EventTypeCreatePost).WithName("EventTypeCreatePost").ApplyJsonModel<EventTypeModel>().WithDescription("Creates or executes EventType resource via /EventTypeResponse/create. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPut("/EventTypeResponse/update/{id:guid}", EventsResolvers.EventTypeUpdatePut).WithName("EventTypeUpdatePut").WithDescription("Updates EventType resource via /EventTypeResponse/update/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapDelete("/EventTypeResponse/delete/{id:guid}", EventsResolvers.EventTypeDeleteDelete).WithName("EventTypeDeleteDelete").WithDescription("Deletes EventType resource via /EventTypeResponse/delete/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPut("/EventResponse/registration-policy/{id:guid}", EventsResolvers.EventRegistrationPolicyPut).WithName("EventRegistrationPolicyPut").WithDescription("Updates Event resource via /EventResponse/registration-policy/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapGet("/EventResponse/participants/{id:guid}", EventsResolvers.EventParticipants).WithName("EventParticipants").WithDescription("Returns Event resource via /EventResponse/participants/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventResponse/invite/{id:guid}", EventsResolvers.EventInvitePost).WithName("EventInvitePost").WithDescription("Creates or executes Event resource via /EventResponse/invite/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventResponse/register/{id:guid}", EventsResolvers.EventRegisterPost).WithName("EventRegisterPost").WithDescription("Creates or executes Event resource via /EventResponse/register/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventParticipantResponse/accept/{id:guid}", EventsResolvers.EventParticipantAcceptPost).WithName("EventParticipantAcceptPost").WithDescription("Creates or executes EventParticipant resource via /EventParticipantResponse/accept/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventParticipantResponse/decline/{id:guid}", EventsResolvers.EventParticipantDeclinePost).WithName("EventParticipantDeclinePost").WithDescription("Creates or executes EventParticipant resource via /EventParticipantResponse/decline/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventResponse/create", EventsResolvers.EventCreatePost).WithName("EventCreatePost").ApplyJsonModel<EventModel>().WithDescription("Creates an event in an authorized scope or under an authorized parent event. Validates event type and time interval; returns created resource and Location.");
        r.MapPut("/EventResponse/update/{id:guid}", EventsResolvers.EventUpdatePut).WithName("EventUpdatePut").WithDescription("Updates a specific event after RBAC and hierarchy checks. Input times are normalized to UTC in the service; responds 204 or 409.");
        r.MapDelete("/EventResponse/delete/{id:guid}", EventsResolvers.EventDeleteDelete).WithName("EventDeleteDelete").WithDescription("Deletes Event resource via /EventResponse/delete/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventParticipantResponse/approve/{id:guid}", EventsResolvers.EventParticipantApprovePost).WithName("EventParticipantApprovePost").WithDescription("Creates or executes EventParticipant resource via /EventParticipantResponse/approve/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
        r.MapPost("/EventParticipantResponse/record-attendance/{id:guid}", EventsResolvers.EventParticipantRecordAttendancePost).WithName("EventParticipantRecordAttendancePost").WithDescription("Creates or executes EventParticipant resource via /EventParticipantResponse/record-attendance/{id:guid}. Requires a signed-in actor; secured operations enforce RBAC. Uses the canonical Events domain contract.");
    }
}
