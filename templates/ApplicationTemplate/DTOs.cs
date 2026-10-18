using System.Linq.Expressions;
using System.Text.Json.Serialization;
using WebAppArchitecture.Base.Data;
namespace RootNamespace.AppName;

public sealed record FirstEntityResponse(Guid id, string Name, Guid rbacobject_id, DateTimeOffset updated_at)
    : IEntityProjection<FirstEntityModel, FirstEntityResponse>, IEntityMapper<FirstEntityModel, FirstEntityResponse>
{
    public static Expression<Func<FirstEntityModel,FirstEntityResponse>> Projection => x => new(x.id,x.Name,x.rbacobject_id,x.updated_at);
    public static FirstEntityResponse FromModel(FirstEntityModel x) => new(x.id,x.Name,x.rbacobject_id,x.updated_at);
    public string visual_label => Name;
}
public sealed record SecondEntityResponse(Guid id, Guid first_entity_id, string Name, Guid rbacobject_id, DateTimeOffset updated_at)
    : IEntityProjection<SecondEntityModel, SecondEntityResponse>, IEntityMapper<SecondEntityModel, SecondEntityResponse>
{
    public static Expression<Func<SecondEntityModel,SecondEntityResponse>> Projection => x => new(x.id,x.first_entity_id,x.Name,x.rbacobject_id,x.updated_at);
    public static SecondEntityResponse FromModel(SecondEntityModel x) => new(x.id,x.first_entity_id,x.Name,x.rbacobject_id,x.updated_at);
    public string visual_label => Name;
}
public sealed record FirstEntityUpdateRequest(Guid id, [property: JsonRequired] DateTimeOffset updated_at, string Name) : IUpdateDTOModel<FirstEntityModel>
{
    public FirstEntityModel ApplyTo(FirstEntityModel entity) => entity with { Name = Name };
}
public sealed record SecondEntityUpdateRequest(Guid id, [property: JsonRequired] DateTimeOffset updated_at, string Name) : IUpdateDTOModel<SecondEntityModel>
{
    public SecondEntityModel ApplyTo(SecondEntityModel entity) => entity with { Name = Name };
}
