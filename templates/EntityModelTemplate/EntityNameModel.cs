using System.Linq.Expressions;
using System.Text.Json.Serialization;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace RootNamespace.EntityName.Models;


public sealed record EntityNameModel(
    Guid id, string Name, Guid rbacobject_id,
    DateTimeOffset created_at, DateTimeOffset updated_at,
    Guid created_by, Guid updated_by) : ISecuredEntityDBModel;


public sealed record EntityNameResponse(Guid id, string Name, Guid rbacobject_id, DateTimeOffset updated_at)
    : IEntityProjection<EntityNameModel, EntityNameResponse>,
      IEntityMapper<EntityNameModel, EntityNameResponse>, ISecuredEntityDTOModel
{
    public static Expression<Func<EntityNameModel, EntityNameResponse>> Projection =>
        entity => new(entity.id, entity.Name, entity.rbacobject_id, entity.updated_at);
    public static EntityNameResponse FromModel(EntityNameModel entity) =>
        new(entity.id, entity.Name, entity.rbacobject_id, entity.updated_at);
    public string visual_label => Name;
}

// Client request supplies a container, not a trusted RBAC object id.
// Conversion into a secured DB model belongs to a service after CreationScopeGuard.
public sealed record EntityNameCreateContainedRequest(string Name, Guid group_id);

public sealed record EntityNameUpdateRequest(Guid id, [property: JsonRequired] DateTimeOffset updated_at, string Name)
    : IUpdateDTOModel<EntityNameModel>
{
    public EntityNameModel ApplyTo(EntityNameModel current) =>
        current with { Name = Name };
}
