using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
namespace RootNamespace.AppName;

public sealed record FirstEntityResponse(Guid id, string Name, Guid rbacobject_id)
    : IEntityProjection<FirstEntityModel, FirstEntityResponse>, IEntityMapper<FirstEntityModel, FirstEntityResponse>
{
    public static Expression<Func<FirstEntityModel,FirstEntityResponse>> Projection => x => new(x.id,x.Name,x.rbacobject_id);
    public static FirstEntityResponse FromModel(FirstEntityModel x) => new(x.id,x.Name,x.rbacobject_id);
    public string visual_label => Name;
}
public sealed record SecondEntityResponse(Guid id, Guid first_entity_id, string Name, Guid rbacobject_id)
    : IEntityProjection<SecondEntityModel, SecondEntityResponse>, IEntityMapper<SecondEntityModel, SecondEntityResponse>
{
    public static Expression<Func<SecondEntityModel,SecondEntityResponse>> Projection => x => new(x.id,x.first_entity_id,x.Name,x.rbacobject_id);
    public static SecondEntityResponse FromModel(SecondEntityModel x) => new(x.id,x.first_entity_id,x.Name,x.rbacobject_id);
    public string visual_label => Name;
}
public sealed record FirstEntityUpdateRequest(Guid id, string Name) : IUpdateDTOModel<FirstEntityModel>
{
    public FirstEntityModel ApplyTo(FirstEntityModel entity) => entity with { Name = Name, updated_at = DateTimeOffset.UtcNow };
}
public sealed record SecondEntityUpdateRequest(Guid id, string Name) : IUpdateDTOModel<SecondEntityModel>
{
    public SecondEntityModel ApplyTo(SecondEntityModel entity) => entity with { Name = Name, updated_at = DateTimeOffset.UtcNow };
}
