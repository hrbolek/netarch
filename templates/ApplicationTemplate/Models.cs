using WebAppArchitecture.Base.Data;
namespace RootNamespace.AppName;

public sealed record FirstEntityModel(Guid id, string Name, Guid rbacobject_id,
    DateTimeOffset created_at, DateTimeOffset updated_at, Guid created_by, Guid updated_by)
    : ISecuredEntityDBModel;
public sealed record SecondEntityModel(Guid id, Guid first_entity_id, string Name, Guid rbacobject_id,
    DateTimeOffset created_at, DateTimeOffset updated_at, Guid created_by, Guid updated_by)
    : ISecuredEntityDBModel;
