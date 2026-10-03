using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.EntityName.Models;

/// <summary>
/// Application/transport DTOs only. The persistence EntityNameModel belongs to DBLayer.
/// </summary>
public sealed record EntityNameResponse(
    Guid id,
    string Name,
    Guid rbac_id
) : IEntityDTOModel<EntityNameModel, EntityNameResponse>, ISecuredEntityDTOModel
{
    public static EntityNameResponse FromModel(EntityNameModel model)
        => new(model.id, model.Name, model.rbac_id);

    public static Expression<Func<EntityNameModel, EntityNameResponse>> Projection
        => model => new EntityNameResponse(model.id, model.Name, model.rbac_id);

    public EntityNameModel ToModel()
        => new(id, Name, rbac_id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.Empty, Guid.Empty);
}

public sealed record EntityNameCreateRequest(
    string Name,
    Guid? rbac_id = null
) : ICreateDTOModel<EntityNameModel>, ISecuredCreateRequest
{
    public EntityNameModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        return new EntityNameModel(Guid.NewGuid(), Name,
            rbac_id ?? WebAppArchitecture.Base.Security.RbacWellKnown.RootSecurityObjectId,
            now, now, Guid.Empty, Guid.Empty);
    }
}

public sealed record EntityNameUpdateRequest(
    Guid id,
    string Name
) : IUpdateDTOModel<EntityNameModel>
{
    public EntityNameModel ApplyTo(EntityNameModel model)
        => model with { Name = Name, updated_at = DateTimeOffset.UtcNow };

    public EntityNameModel ToModel()
        => new(id, Name, WebAppArchitecture.Base.Security.RbacWellKnown.RootSecurityObjectId,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.Empty, Guid.Empty);
}
