using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.Facilities;

public interface IFacilityService : ISecuredEntityService<FacilityModel>
{
    Task<IReadOnlyList<FacilityResponse>> ListAsync(string? search = null, int? skip = null, int? take = null, CancellationToken ct = default);
    Task<FacilityResponse> CreateAsync(FacilityCreateRequest request, CancellationToken ct = default);
    Task<FacilityResponse> UpdateAsync(FacilityUpdateRequest request, CancellationToken ct = default);
    /// <summary>Requires literal move on the root, Create on a non-null destination, and copies/rebinds the owned permission subtree atomically.</summary>
    Task<FacilityResponse> MoveAsync(Guid id, FacilityMoveRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, FacilityDeleteRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<FacilityResponse>> ListChildrenAsync(Guid parentId, int? skip = null, int? take = null, CancellationToken ct = default);
    Task<IReadOnlyList<FacilityResponse>> ListAncestorsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FacilityResponse>> ListDescendantsAsync(Guid id, int? skip = null, int? take = null, CancellationToken ct = default);
}

public interface IFacilityTypeService : ISecuredEntityService<FacilityTypeModel>
{
    Task<IReadOnlyList<FacilityTypeResponse>> ListAsync(string? search = null, int? skip = null, int? take = null, CancellationToken ct = default);
    Task<FacilityTypeResponse> CreateAsync(FacilityTypeCreateRequest request, CancellationToken ct = default);
    Task<FacilityTypeResponse> UpdateAsync(FacilityTypeUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, FacilityDeleteRequest request, CancellationToken ct = default);
}

public interface IReservationService : ISecuredEntityService<ReservationModel>
{
    Task<ReservationResponse> RequestAsync(ReservationRequest request, CancellationToken ct = default);
    Task<ReservationResponse> ConfirmAsync(Guid id, ReservationTransitionRequest request, CancellationToken ct = default);
    Task<ReservationResponse> RejectAsync(Guid id, ReservationTransitionRequest request, CancellationToken ct = default);
    Task<ReservationResponse> CancelAsync(Guid id, ReservationTransitionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationResponse>> ListByFacilityAsync(Guid facilityId, int? skip = null, int? take = null, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationResponse>> ListByEventAsync(Guid eventId, int? skip = null, int? take = null, CancellationToken ct = default);
    /// <summary>Distinct readable facilities linked by readable reservation history, including requested, confirmed, rejected and cancelled reservations.</summary>
    Task<IReadOnlyList<FacilityResponse>> ListFacilitiesByEventAsync(Guid eventId, int? skip = null, int? take = null, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationResponse>> OccupancyAsync(Guid facilityId, DateTimeOffset start, DateTimeOffset end, int? skip = null, int? take = null, CancellationToken ct = default);
    Task<FacilityAvailabilityResponse> AvailabilityAsync(Guid facilityId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default);
}
