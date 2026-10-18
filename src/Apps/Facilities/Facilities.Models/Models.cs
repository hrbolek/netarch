using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Facilities;

public abstract class FacilityEntity : ISecuredEntityDBModel
{
    public Guid id { get; set; } = Guid.NewGuid();
    public Guid rbacobject_id { get; set; }
    public DateTimeOffset created_at { get; set; }
    public DateTimeOffset updated_at { get; set; }
    public Guid created_by { get; set; }
    public Guid updated_by { get; set; }
}

public sealed class FacilityModel : FacilityEntity, ITreeEntityDBModel<FacilityModel>
{
    public Guid? ParentId { get; set; }
    public Guid FacilityTypeId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public string path { get; set; } = "";
    Guid? ITreeEntityDBModel<FacilityModel>.parent_id => ParentId;
    public FacilityModel WithPath(string value) { path = value; return this; }
}

public sealed class FacilityTypeModel : FacilityEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool Reservable { get; set; }
    /// <summary>False denies all children, even if AllowedChildTypes is empty or populated.</summary>
    public bool CanContainChildren { get; set; } = true;
    /// <summary>Empty means unrestricted child types when CanContainChildren is true.</summary>
    public ICollection<FacilityTypeAllowedChildModel> AllowedChildTypes { get; set; } = new List<FacilityTypeAllowedChildModel>();
}

/// <summary>Configuration owned by its parent type, not an independently secured entity.</summary>
public sealed class FacilityTypeAllowedChildModel
{
    public Guid ParentTypeId { get; set; }
    public Guid ChildTypeId { get; set; }
}

public static class ReservationStatuses
{
    public const string Requested = "requested";
    public const string Confirmed = "confirmed";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";
}

public sealed class ReservationModel : FacilityEntity
{
    public Guid FacilityId { get; set; }
    public Guid EventId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Status { get; set; } = ReservationStatuses.Requested;
    public string? Note { get; set; }
    public string? DecisionReason { get; set; }
}
