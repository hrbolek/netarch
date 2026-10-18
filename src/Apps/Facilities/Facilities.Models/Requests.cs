using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Facilities;

public sealed record FacilityCreateRequest(
    string Name, string? Description, Guid FacilityTypeId, Guid? ParentId,
    bool IsActive, Guid RbacObjectId, string Code = "", int? Capacity = null) : ICreateDTOModel<FacilityModel>
{
    /// <summary>For roots, RbacObjectId selects the existing permission parent; it is never the assigned Facility scope.</summary>
    public FacilityModel ToModel() => new() { Name = Name, Description = Description,
        FacilityTypeId = FacilityTypeId, ParentId = ParentId, IsActive = IsActive,
        rbacobject_id = RbacObjectId, Code = Code, Capacity = Capacity };
}

public sealed record FacilityUpdateRequest(
    Guid id, DateTimeOffset updated_at, string Name, string? Description,
    Guid FacilityTypeId, bool IsActive, string Code = "", int? Capacity = null) : IUpdateDTOModel<FacilityModel>
{
    public FacilityModel ApplyTo(FacilityModel x)
    {
        x.Name = Name; x.Description = Description; x.FacilityTypeId = FacilityTypeId; x.IsActive = IsActive;
        x.Code = Code; x.Capacity = Capacity;
        return x;
    }
}

public sealed record FacilityMoveRequest(Guid? ParentId, DateTimeOffset updated_at);
public sealed record FacilityDeleteRequest(DateTimeOffset updated_at);

public sealed record FacilityTypeCreateRequest(
    string Code, string Name, string? Description, bool Reservable, bool CanContainChildren,
    Guid[] AllowedChildTypeIds, Guid RbacObjectId) : ICreateDTOModel<FacilityTypeModel>
{
    public FacilityTypeModel ToModel() => new() { Code = Code, Name = Name, Description = Description,
        Reservable = Reservable, CanContainChildren = CanContainChildren, rbacobject_id = RbacObjectId };
}

public sealed record FacilityTypeUpdateRequest(
    Guid id, DateTimeOffset updated_at, string Code, string Name, string? Description,
    bool Reservable, bool CanContainChildren, Guid[] AllowedChildTypeIds) : IUpdateDTOModel<FacilityTypeModel>
{
    public FacilityTypeModel ApplyTo(FacilityTypeModel x)
    {
        x.Code = Code; x.Name = Name; x.Description = Description;
        x.Reservable = Reservable; x.CanContainChildren = CanContainChildren;
        return x;
    }
}

public sealed record ReservationRequest(
    Guid FacilityId, Guid EventId, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    Guid RbacObjectId, string? Note = null) : ICreateDTOModel<ReservationModel>
{
    public ReservationModel ToModel() => new() { FacilityId = FacilityId, EventId = EventId,
        StartsAt = StartsAt.ToUniversalTime(), EndsAt = EndsAt.ToUniversalTime(),
        rbacobject_id = RbacObjectId, Note = Note };
}

public sealed record ReservationTransitionRequest(DateTimeOffset updated_at, string? Reason = null);
