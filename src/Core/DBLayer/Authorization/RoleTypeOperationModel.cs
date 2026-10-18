using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Typ Role Type Operation ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>

public sealed record RoleTypeOperationModel(
    Guid id,
    Guid role_type_id,
    Guid operation_id
) : IEntityDBModel
{
    public OperationModel? operation { get; init; }
}


/// <summary>
/// Declarative mapping from a functional role valid at source_group_id to an operational
/// role over target_rbacobject_id. The binding contains no concrete user id.
/// SecurityProjectionService resolves current role holders into EffectiveAccess.
/// </summary>
