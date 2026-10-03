using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.RoleType.Models;

/// <summary>Catalog of authorization operations. New operations are data, not schema changes.</summary>
public sealed record OperationModel(
    Guid id,
    string name,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDBModel;

/// <summary>Normalized many-to-many mapping RoleType -> Operation.</summary>
public sealed record RoleTypeOperationModel(
    Guid id,
    Guid role_type_id,
    Guid operation_id
) : IEntityDBModel
{
    public OperationModel? operation { get; init; }
}
