using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;

namespace WebAppArchitecture.Facilities;

public static class FacilitiesEndpointExtensions
{
    public static void MapApi(IEndpointRouteBuilder endpoints, string prefix)
        => MapRoutes(endpoints, prefix, ui: false);

    public static void MapUi(IEndpointRouteBuilder endpoints, string prefix)
        => MapRoutes(endpoints, prefix, ui: true);

    private static void MapRoutes(IEndpointRouteBuilder endpoints, string prefix, bool ui)
    {
        var facilities = endpoints.MapGroup($"/{prefix.Trim('/')}/FacilityResponse").WithTags("Facilities");
        if (ui)
        {
            facilities.MapGet("/list", FacilitiesResolvers.FacilityListPage)
                .WithName("FacilityListPage")
                .ApplyViewModel<FacilityListPage, IReadOnlyList<FacilityResponse>>()
                .WithCommonErrors()
                .WithDescription("Lists facilities the actor may read, optionally filtered by name and paged with skip/take. JSON and HTML representations share the secured service result.");
            facilities.MapGet("/tree", FacilitiesResolvers.FacilityTreePage)
                .WithName("FacilityTreePage")
                .ApplyViewModel<FacilityTreePage, IReadOnlyList<FacilityResponse>>()
                .WithCommonErrors()
                .WithDescription("Returns a secured flat tree projection with parent IDs and materialized paths; supports search and paging and renders the FacilityTreePage.");
            facilities.MapGet("/detail/{id:guid}", FacilitiesResolvers.FacilityDetailPage)
                .WithName("FacilityDetailPage")
                .WithDescription("Renders a facility, readable children, and readable reservations after service-layer authorization. Returns 401 for an absent actor, 404 for an unavailable facility, and 403 on related-access denial.")
                .Produces(StatusCodes.Status200OK, contentType: "text/html")
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict);
            facilities.MapGet("/edit/{id:guid}", FacilitiesResolvers.FacilityEditPage)
                .WithName("FacilityEditPage")
                .WithDescription("Renders the facility edit form with its readable type choices; returns 401, 403, or 404 when the actor or requested resource is unavailable.")
                .Produces(StatusCodes.Status200OK, contentType: "text/html")
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict);
            facilities.MapGet("/new", FacilitiesResolvers.FacilityCreatePage)
                .WithName("FacilityCreatePage")
                    .WithDescription("Renders the facility creation form. For a root, the selected permission parent must grant Create; for a child, its Facility parent must grant Create. The service creates an owned Group beneath that parent.")
                .Produces(StatusCodes.Status200OK, contentType: "text/html")
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);
        }
        else
        {
            facilities.MapGet("/list", FacilitiesResolvers.FacilityList)
                .WithName("FacilityList")
                .ApplyJsonModel<IReadOnlyList<FacilityResponse>>()
                .WithCommonErrors()
                .WithDescription("Lists facilities the actor may read, optionally filtered by name and paged with skip/take. Missing actor returns 401; denied or invalid requests return 403/400.");
            facilities.MapGet("/tree", FacilitiesResolvers.FacilityTree)
                .WithName("FacilityTree")
                .ApplyJsonModel<IReadOnlyList<FacilityResponse>>()
                .WithCommonErrors()
                .WithDescription("Returns the authorized flat facility tree projection, including parent IDs and materialized paths; supports search and paging. Access is enforced by the facility service.");
        }
        facilities.MapGet("/view/{id:guid}", FacilitiesResolvers.FacilityRead)
            .WithName("FacilityRead")
            .ApplyJsonModel<FacilityResponse>()
            .WithCommonErrors()
            .WithDescription("Returns one facility DTO visible to the actor; returns 404 when unavailable and 401 when no trusted actor is present.");
        facilities.MapPost("/create", FacilitiesResolvers.FacilityCreate)
            .WithName("FacilityCreate")
            .ApplyJsonModel<FacilityResponse>()
            .WithCommonErrors()
            .WithDescription("Creates a Facility with its own Group-based RBAC scope beneath the selected permission parent when creating a root, or beneath its Facility parent when creating a child. The actor must have Create permission on that parent. Invalid data returns 400, denied access 403, and structural conflicts 409.");
        facilities.MapPut("/update", FacilitiesResolvers.FacilityUpdate)
            .WithName("FacilityUpdate")
            .ApplyJsonModel<FacilityResponse>()
            .WithCommonErrors()
            .WithDescription("Updates a facility using its updated_at concurrency token; service authorization and hierarchy/type validation apply. Invalid data returns 400 and conflicts return 409.");
        facilities.MapPut("/update/{id:guid}", FacilitiesResolvers.FacilityUpdateById)
            .WithName("FacilityUpdateById")
            .ApplyJsonModel<FacilityResponse>()
            .WithCommonErrors()
            .WithDescription("Updates the identified facility using its updated_at token; the body ID must match the route ID. Authorization, field validation, and hierarchy invariants are enforced by the service.");
        facilities.MapPost("/move/{id:guid}", FacilitiesResolvers.FacilityMove)
            .WithName("FacilityMove")
            .ApplyJsonModel<FacilityResponse>()
            .WithCommonErrors()
            .WithDescription("Moves the Facility subtree to a destination where the actor has Create permission, and requires the dedicated literal move permission on the source root. The service copies the source owned RBAC subtree, including roles and memberships, beneath the destination; it remaps Facilities to the copied scopes and recalculates the copied hierarchy atomically while leaving the old tree unchanged. Requires the root updated_at token; invalid moves return 400 and state conflicts return 409.");
        facilities.MapDelete("/delete/{id:guid}", FacilitiesResolvers.FacilityDelete)
            .WithName("FacilityDelete")
            .WithCommonErrors()
            .WithDescription("Deletes a leaf facility without reservation history using an updated_at token. Returns 204, 401/403, or 409 for a protected or structurally conflicting operation.")
            .Produces(StatusCodes.Status204NoContent);
        facilities.MapGet("/children/{id:guid}", FacilitiesResolvers.FacilityChildren)
            .WithName("FacilityChildren")
            .ApplyJsonModel<IReadOnlyList<FacilityResponse>>()
            .WithCommonErrors()
            .WithDescription("Lists direct children the actor may read with optional skip/take paging. The service scopes the parent and every child.");
        facilities.MapGet("/ancestors/{id:guid}", FacilitiesResolvers.FacilityAncestors)
            .WithName("FacilityAncestors")
            .ApplyJsonModel<IReadOnlyList<FacilityResponse>>()
            .WithCommonErrors()
            .WithDescription("Returns readable ancestors in root-to-parent order; unreadable ancestors are omitted by the secured tree service.");
        facilities.MapGet("/descendants/{id:guid}", FacilitiesResolvers.FacilityDescendants)
            .WithName("FacilityDescendants")
            .ApplyJsonModel<IReadOnlyList<FacilityResponse>>()
            .WithCommonErrors()
            .WithDescription("Lists readable descendants, excluding the requested facility, with optional skip/take paging.");

        var types = endpoints.MapGroup($"/{prefix.Trim('/')}/FacilityTypeResponse").WithTags("FacilityTypes");
        if (ui)
        {
            types.MapGet("/list", FacilitiesResolvers.FacilityTypeListPage)
                .WithName("FacilityTypeListPage")
                .ApplyViewModel<FacilityTypeAdminPage, IReadOnlyList<FacilityTypeResponse>>()
                .WithCommonErrors()
                .WithDescription("Lists facility types visible to the actor, optionally filtered by name/code and paged with skip/take; supports JSON and HTML.");
            types.MapGet("/edit/{id:guid}", FacilitiesResolvers.FacilityTypeEditPage)
                .WithName("FacilityTypeEditPage")
                .WithDescription("Renders the authorized facility type edit form with readable child-type choices; unavailable types return 404.")
                .Produces(StatusCodes.Status200OK, contentType: "text/html")
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict);
            types.MapGet("/new", FacilitiesResolvers.FacilityTypeCreatePage)
                .WithName("FacilityTypeCreatePage")
                .WithDescription("Renders the facility type creation form with readable child-type choices; explicit scope is checked by the create service.")
                .Produces(StatusCodes.Status200OK, contentType: "text/html")
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);
        }
        else
        {
            types.MapGet("/list", FacilitiesResolvers.FacilityTypeList)
                .WithName("FacilityTypeList")
                .ApplyJsonModel<IReadOnlyList<FacilityTypeResponse>>()
                .WithCommonErrors()
                .WithDescription("Lists facility types visible to the actor, optionally filtered by name/code and paged with skip/take.");
        }
        types.MapGet("/view/{id:guid}", FacilitiesResolvers.FacilityTypeRead)
            .WithName("FacilityTypeRead")
            .ApplyJsonModel<FacilityTypeResponse>()
            .WithCommonErrors()
            .WithDescription("Returns one readable facility type and its allowed-child type IDs; unavailable types return 404.");
        types.MapPost("/create", FacilitiesResolvers.FacilityTypeCreate)
            .WithName("FacilityTypeCreate")
            .ApplyJsonModel<FacilityTypeResponse>()
            .WithCommonErrors()
            .WithDescription("Creates a facility type and its allowed-child rules in an explicit RBAC scope; invalid data returns 400 and code conflicts return 409.");
        types.MapPut("/update", FacilitiesResolvers.FacilityTypeUpdate)
            .WithName("FacilityTypeUpdate")
            .ApplyJsonModel<FacilityTypeResponse>()
            .WithCommonErrors()
            .WithDescription("Updates a facility type and its allowed-child rules using updated_at; in-use compatibility or code conflicts return 409.");
        types.MapPut("/update/{id:guid}", FacilitiesResolvers.FacilityTypeUpdateById)
            .WithName("FacilityTypeUpdateById")
            .ApplyJsonModel<FacilityTypeResponse>()
            .WithCommonErrors()
            .WithDescription("Updates the identified facility type using its updated_at token; the body ID must match the route ID.");
        types.MapDelete("/delete/{id:guid}", FacilitiesResolvers.FacilityTypeDelete)
            .WithName("FacilityTypeDelete")
            .WithCommonErrors()
            .WithDescription("Deletes an unused facility type using an updated_at token; returns 204, 401/403, or 409 when still in use.")
            .Produces(StatusCodes.Status204NoContent);

        var reservations = endpoints.MapGroup($"/{prefix.Trim('/')}/FacilityReservationResponse").WithTags("FacilityReservations");
        reservations.MapPost("/request", FacilitiesResolvers.ReservationRequest)
            .WithName("FacilityReservationRequest")
            .ApplyJsonModel<ReservationResponse>()
            .WithCommonErrors()
            .WithDescription("Requests an event reservation. The service requires Read on the facility, Update on the event, and Create on the explicit reservation scope; conflicts return 409.");
        reservations.MapPost("/confirm/{id:guid}", FacilitiesResolvers.ReservationConfirm)
            .WithName("FacilityReservationConfirm")
            .ApplyJsonModel<ReservationResponse>()
            .WithCommonErrors()
            .WithDescription("Confirms a pending reservation after Update authorization on the facility, event, and reservation; unavailable intervals or stale tokens return 409.");
        reservations.MapPost("/reject/{id:guid}", FacilitiesResolvers.ReservationReject)
            .WithName("FacilityReservationReject")
            .ApplyJsonModel<ReservationResponse>()
            .WithCommonErrors()
            .WithDescription("Rejects a pending reservation after Update authorization on the facility, event, and reservation; stale tokens or invalid transitions return 409.");
        reservations.MapPost("/cancel/{id:guid}", FacilitiesResolvers.ReservationCancel)
            .WithName("FacilityReservationCancel")
            .ApplyJsonModel<ReservationResponse>()
            .WithCommonErrors()
            .WithDescription("Cancels a pending or confirmed reservation after service-layer authorization and concurrency checks; stale tokens or invalid transitions return 409.");
        if (ui)
        {
            reservations.MapGet("/by-facility/{facilityId:guid}", FacilitiesResolvers.ReservationOverviewPage)
                .WithName("FacilityReservationsByFacilityPage")
                .ApplyViewModel<ReservationOverviewPage, IReadOnlyList<ReservationResponse>>()
                .WithCommonErrors()
                .WithDescription("Renders the reservation overview for one readable facility; each returned reservation, its facility, and its event must be readable.");
            reservations.MapGet("/new", FacilitiesResolvers.ReservationRequestPage)
                .WithName("FacilityReservationRequestPage")
                .WithDescription("Renders the reservation request form. The request endpoint separately enforces facility Read, event Update, and explicit-scope Create authorization.")
                .Produces(StatusCodes.Status200OK, contentType: "text/html")
                .Produces(StatusCodes.Status401Unauthorized);
        }
        else
        {
            reservations.MapGet("/by-facility/{facilityId:guid}", FacilitiesResolvers.ReservationsByFacility)
                .WithName("FacilityReservationsByFacility")
                .ApplyJsonModel<IReadOnlyList<ReservationResponse>>()
                .WithCommonErrors()
                .WithDescription("Lists reservations only when Read access is available for the facility, event, and each reservation; supports skip/take paging.");
        }
        reservations.MapGet("/by-event/{eventId:guid}", FacilitiesResolvers.ReservationsByEvent)
            .WithName("FacilityReservationsByEvent")
            .ApplyJsonModel<IReadOnlyList<ReservationResponse>>()
            .WithCommonErrors()
            .WithDescription("Lists reservations only when Read access is available for the facility, event, and each reservation; supports skip/take paging.");
        reservations.MapGet("/facilities-by-event/{eventId:guid}", FacilitiesResolvers.FacilitiesByEvent)
            .WithName("FacilitiesByEvent")
            .ApplyJsonModel<IReadOnlyList<FacilityResponse>>()
            .WithCommonErrors()
            .WithDescription("Returns distinct readable facilities linked through readable reservation history for the event, across all reservation statuses; supports skip/take paging.");
        reservations.MapGet("/availability/{facilityId:guid}", FacilitiesResolvers.FacilityAvailability)
            .WithName("FacilityAvailability")
            .ApplyJsonModel<FacilityAvailabilityResponse>()
            .WithCommonErrors()
            .WithDescription("Reports availability for a half-open start/end interval without disclosing reservation identities; invalid intervals return 400.");
        reservations.MapGet("/occupancy/{facilityId:guid}", FacilitiesResolvers.FacilityOccupancy)
            .WithName("FacilityOccupancy")
            .ApplyJsonModel<IReadOnlyList<ReservationResponse>>()
            .WithCommonErrors()
            .WithDescription("Lists confirmed reservations overlapping the requested half-open interval, after Read authorization on facility, event, and reservation; supports skip/take.");
    }

    private static RouteHandlerBuilder WithCommonErrors(this RouteHandlerBuilder builder)
        => builder
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
