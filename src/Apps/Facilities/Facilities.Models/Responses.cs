using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Facilities;

public sealed record FacilityResponse(
    Guid id, Guid? ParentId, Guid FacilityTypeId, string Name, string? Description,
    bool IsActive, string path, Guid rbacobject_id, DateTimeOffset created_at,
    DateTimeOffset updated_at, Guid created_by, Guid updated_by, string Code = "", int? Capacity = null)
    : ISecuredEntityDTOModel, IEntityProjection<FacilityModel, FacilityResponse>, IEntityMapper<FacilityModel, FacilityResponse>
{
    public string visual_label => Name;
    public static Expression<Func<FacilityModel, FacilityResponse>> Projection => x => new(
        x.id, x.ParentId, x.FacilityTypeId, x.Name, x.Description, x.IsActive, x.path,
        x.rbacobject_id, x.created_at, x.updated_at, x.created_by, x.updated_by, x.Code, x.Capacity);
    public static FacilityResponse FromModel(FacilityModel x) => new(
        x.id, x.ParentId, x.FacilityTypeId, x.Name, x.Description, x.IsActive, x.path,
        x.rbacobject_id, x.created_at, x.updated_at, x.created_by, x.updated_by, x.Code, x.Capacity);
}

public sealed record FacilityTypeResponse(
    Guid id, string Code, string Name, string? Description, bool Reservable,
    bool CanContainChildren, Guid[] AllowedChildTypeIds, Guid rbacobject_id,
    DateTimeOffset created_at, DateTimeOffset updated_at, Guid created_by, Guid updated_by)
    : ISecuredEntityDTOModel, IEntityProjection<FacilityTypeModel, FacilityTypeResponse>, IEntityMapper<FacilityTypeModel, FacilityTypeResponse>
{
    public string visual_label => Name;
    public static Expression<Func<FacilityTypeModel, FacilityTypeResponse>> Projection => x => new(
        x.id, x.Code, x.Name, x.Description, x.Reservable, x.CanContainChildren,
        x.AllowedChildTypes.Select(c => c.ChildTypeId).ToArray(),
        x.rbacobject_id, x.created_at, x.updated_at, x.created_by, x.updated_by);
    public static FacilityTypeResponse FromModel(FacilityTypeModel x) => new(
        x.id, x.Code, x.Name, x.Description, x.Reservable, x.CanContainChildren,
        x.AllowedChildTypes.Select(c => c.ChildTypeId).ToArray(),
        x.rbacobject_id, x.created_at, x.updated_at, x.created_by, x.updated_by);
}

public sealed record ReservationResponse(
    Guid id, Guid FacilityId, Guid EventId, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    string Status, string? Note, string? DecisionReason, Guid rbacobject_id,
    DateTimeOffset created_at, DateTimeOffset updated_at, Guid created_by, Guid updated_by)
    : ISecuredEntityDTOModel, IEntityProjection<ReservationModel, ReservationResponse>, IEntityMapper<ReservationModel, ReservationResponse>
{
    public string visual_label => $"{Status}: {StartsAt:u} – {EndsAt:u}";
    public static Expression<Func<ReservationModel, ReservationResponse>> Projection => x => new(
        x.id, x.FacilityId, x.EventId, x.StartsAt, x.EndsAt, x.Status, x.Note, x.DecisionReason,
        x.rbacobject_id, x.created_at, x.updated_at, x.created_by, x.updated_by);
    public static ReservationResponse FromModel(ReservationModel x) => new(
        x.id, x.FacilityId, x.EventId, x.StartsAt, x.EndsAt, x.Status, x.Note, x.DecisionReason,
        x.rbacobject_id, x.created_at, x.updated_at, x.created_by, x.updated_by);
}

/// <summary>No reservation identities or hidden event details are disclosed by availability.</summary>
public sealed record FacilityAvailabilityResponse(Guid FacilityId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, bool IsAvailable);
