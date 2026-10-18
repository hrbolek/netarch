using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Facilities;

public static class FacilitiesResolvers
{
    public static Task<object> FacilityList(HttpContext http, ICurrentActorProvider actors, IFacilityService service,
        string? search, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListAsync(search, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilityTree(HttpContext http, ICurrentActorProvider actors, IFacilityService service,
        string? search, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListAsync(search, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilityRead(Guid id, HttpContext http, ICurrentActorProvider actors,
        IFacilityService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.GetByIdAsync<FacilityResponse>(id, ct));

    public static Task<object> FacilityCreate(FacilityCreateRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.CreateAsync(request, ct));

    public static Task<object> FacilityUpdate(FacilityUpdateRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.UpdateAsync(request, ct));

    public static Task<object> FacilityUpdateById(Guid id, FacilityUpdateRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityService service, CancellationToken ct)
        => ExecuteModel(http, actors, async () =>
        {
            if (request.id != id)
                throw new ArgumentException("The route and body IDs must match.");
            return await service.UpdateAsync(request, ct);
        });

    public static Task<object> FacilityMove(Guid id, FacilityMoveRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.MoveAsync(id, request, ct));

    public static Task<IResult> FacilityDelete(Guid id, [FromBody] FacilityDeleteRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityService service, CancellationToken ct)
        => Execute(http, actors, async () =>
        {
            await service.DeleteAsync(id, request, ct);
            return Results.NoContent();
        });

    public static Task<object> FacilityChildren(Guid id, HttpContext http, ICurrentActorProvider actors,
        IFacilityService service, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListChildrenAsync(id, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilityAncestors(Guid id, HttpContext http, ICurrentActorProvider actors,
        IFacilityService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListAncestorsAsync(id, ct), rejectOperation: true);

    public static Task<object> FacilityDescendants(Guid id, HttpContext http, ICurrentActorProvider actors,
        IFacilityService service, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListDescendantsAsync(id, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilityTypeList(HttpContext http, ICurrentActorProvider actors,
        IFacilityTypeService service, string? search, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListAsync(search, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilityTypeRead(Guid id, HttpContext http, ICurrentActorProvider actors,
        IFacilityTypeService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.GetByIdAsync<FacilityTypeResponse>(id, ct));

    public static Task<object> FacilityTypeCreate(FacilityTypeCreateRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityTypeService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.CreateAsync(request, ct));

    public static Task<object> FacilityTypeUpdate(FacilityTypeUpdateRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityTypeService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.UpdateAsync(request, ct));

    public static Task<object> FacilityTypeUpdateById(Guid id, FacilityTypeUpdateRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityTypeService service, CancellationToken ct)
        => ExecuteModel(http, actors, async () =>
        {
            if (request.id != id)
                throw new ArgumentException("The route and body IDs must match.");
            return await service.UpdateAsync(request, ct);
        });

    public static Task<IResult> FacilityTypeDelete(Guid id, [FromBody] FacilityDeleteRequest request, HttpContext http,
        ICurrentActorProvider actors, IFacilityTypeService service, CancellationToken ct)
        => Execute(http, actors, async () =>
        {
            await service.DeleteAsync(id, request, ct);
            return Results.NoContent();
        });

    public static Task<object> ReservationRequest(ReservationRequest request, HttpContext http,
        ICurrentActorProvider actors, IReservationService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.RequestAsync(request, ct));

    public static Task<object> ReservationConfirm(Guid id, ReservationTransitionRequest request, HttpContext http,
        ICurrentActorProvider actors, IReservationService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ConfirmAsync(id, request, ct));

    public static Task<object> ReservationReject(Guid id, ReservationTransitionRequest request, HttpContext http,
        ICurrentActorProvider actors, IReservationService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.RejectAsync(id, request, ct));

    public static Task<object> ReservationCancel(Guid id, ReservationTransitionRequest request, HttpContext http,
        ICurrentActorProvider actors, IReservationService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.CancelAsync(id, request, ct));

    public static Task<object> ReservationsByFacility(Guid facilityId, HttpContext http,
        ICurrentActorProvider actors, IReservationService service, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListByFacilityAsync(facilityId, skip, take, ct), rejectOperation: true);

    public static Task<object> ReservationsByEvent(Guid eventId, HttpContext http,
        ICurrentActorProvider actors, IReservationService service, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListByEventAsync(eventId, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilitiesByEvent(Guid eventId, HttpContext http,
        ICurrentActorProvider actors, IReservationService service, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListFacilitiesByEventAsync(eventId, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilityAvailability(Guid facilityId, DateTimeOffset startsAt, DateTimeOffset endsAt,
        HttpContext http, ICurrentActorProvider actors, IReservationService service, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.AvailabilityAsync(facilityId, startsAt, endsAt, ct));

    public static Task<object> FacilityOccupancy(Guid facilityId, DateTimeOffset startsAt, DateTimeOffset endsAt,
        HttpContext http, ICurrentActorProvider actors, IReservationService service, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.OccupancyAsync(facilityId, startsAt, endsAt, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilityListPage(HttpContext http, ICurrentActorProvider actors, IFacilityService service,
        string? search, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListAsync(search, skip, take, ct), rejectOperation: true);

    public static Task<object> FacilityTreePage(HttpContext http, ICurrentActorProvider actors, IFacilityService service,
        string? search, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListAsync(search, skip, take, ct), rejectOperation: true);

    public static Task<IResult> FacilityDetailPage(Guid id, HttpContext http, ICurrentActorProvider actors,
        IFacilityService facilities, IReservationService reservations, CancellationToken ct)
        => Execute(http, actors, async () =>
        {
            var model = await facilities.GetByIdAsync<FacilityResponse>(id, ct);
            if (model is null)
                return Results.NotFound();
            var children = await facilities.ListChildrenAsync(id, ct: ct);
            var bookings = await reservations.ListByFacilityAsync(id, ct: ct);
            return new RazorComponentResult<FacilityDetailPage>(new Dictionary<string, object?>
            {
                ["Model"] = model,
                ["Children"] = children,
                ["Reservations"] = bookings
            });
        });

    public static Task<IResult> FacilityEditPage(Guid id, HttpContext http, ICurrentActorProvider actors,
        IFacilityService facilities, IFacilityTypeService types, CancellationToken ct)
        => Execute(http, actors, async () =>
        {
            var model = await facilities.GetByIdAsync<FacilityResponse>(id, ct);
            if (model is null)
                return Results.NotFound();
            var typeModels = await types.ListAsync(take: 200, ct: ct);
            return new RazorComponentResult<FacilityEditPage>(new Dictionary<string, object?>
            {
                ["Model"] = model,
                ["Types"] = typeModels
            });
        });

    public static Task<IResult> FacilityCreatePage(HttpContext http, ICurrentActorProvider actors)
        => Execute(http, actors, () => Task.FromResult<IResult>(new RazorComponentResult<FacilityCreatePage>()));

    public static Task<object> FacilityTypeListPage(HttpContext http, ICurrentActorProvider actors,
        IFacilityTypeService service, string? search, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, () => service.ListAsync(search, skip, take, ct), rejectOperation: true);

    public static Task<IResult> FacilityTypeEditPage(Guid id, HttpContext http, ICurrentActorProvider actors,
        IFacilityTypeService service, CancellationToken ct)
        => Execute(http, actors, async () =>
        {
            var model = await service.GetByIdAsync<FacilityTypeResponse>(id, ct);
            if (model is null)
                return Results.NotFound();
            var typeModels = await service.ListAsync(take: 200, ct: ct);
            return new RazorComponentResult<FacilityTypeEditPage>(new Dictionary<string, object?>
            {
                ["Model"] = model,
                ["Types"] = typeModels
            });
        });

    public static Task<IResult> FacilityTypeCreatePage(HttpContext http, ICurrentActorProvider actors,
        IFacilityTypeService service, CancellationToken ct)
        => Execute(http, actors, async () =>
        {
            var typeModels = await service.ListAsync(take: 200, ct: ct);
            return new RazorComponentResult<FacilityTypeCreatePage>(new Dictionary<string, object?>
            {
                ["Types"] = typeModels
            });
        });

    public static Task<object> ReservationOverviewPage(Guid facilityId, HttpContext http,
        ICurrentActorProvider actors, IReservationService service, int? skip, int? take, CancellationToken ct)
        => ExecuteModel(http, actors, async () => await service.ListByFacilityAsync(facilityId, skip, take, ct));

    public static Task<IResult> ReservationRequestPage(HttpContext http, ICurrentActorProvider actors)
        => Execute(http, actors, () => Task.FromResult<IResult>(new RazorComponentResult<ReservationRequestPage>()));

    private static async Task<IResult> Execute(HttpContext http, ICurrentActorProvider actors, Func<Task<IResult>> operation)
    {
        if (actors.TryGetActorUserId(http) is null)
            return Results.Unauthorized();

        try
        {
            return await operation();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Invalid request", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Resource not found", Detail = exception.Message });
        }
        catch (DbUpdateConcurrencyException exception)
        {
            return Results.Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Resource changed", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Operation conflicts with current state", Detail = exception.Message });
        }
    }

    private static async Task<object> ExecuteModel<T>(
        HttpContext http,
        ICurrentActorProvider actors,
        Func<Task<T>> operation,
        bool rejectOperation = false)
    {
        if (actors.TryGetActorUserId(http) is null)
            return Results.Unauthorized();
        if (rejectOperation && http.Request.Query.ContainsKey("operation"))
            return Results.BadRequest(new { error = "The RBAC operation is fixed for this endpoint." });

        try
        {
            var model = await operation();
            return model is null ? Results.NotFound() : model;
        }
        catch (UnauthorizedAccessException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Invalid request", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Resource not found", Detail = exception.Message });
        }
        catch (DbUpdateConcurrencyException exception)
        {
            return Results.Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Resource changed", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Operation conflicts with current state", Detail = exception.Message });
        }
    }
}
