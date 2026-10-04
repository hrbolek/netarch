using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Models;

public sealed record RoleTypeResponse(
    Guid id,
    string name,
    string[] operations,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDTOModel<RoleTypeModel, RoleTypeResponse>, ISecuredEntityDTOModel
{
    public static RoleTypeResponse FromModel(RoleTypeModel model)
        => new(model.id, model.name,
            model.operation_links.Where(x => x.operation is not null).Select(x => x.operation!.name).OrderBy(x => x).ToArray(),
            model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by);

    public static Expression<Func<RoleTypeModel, RoleTypeResponse>> Projection => model => new RoleTypeResponse(
        model.id, model.name,
        model.operation_links.Where(x => x.operation != null).Select(x => x.operation!.name).OrderBy(x => x).ToArray(),
        model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by);

    public RoleTypeModel ToModel() => new(id, name, rbacobject_id, created_at, updated_at, created_by, updated_by)
    {
        requested_operations = operations
    };
}

public sealed record RoleTypeCreateRequest(
    string name,
    string[] operations,
    Guid? rbacobject_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<RoleTypeModel>, ISecuredCreateRequest
{
    public RoleTypeModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        return new RoleTypeModel(Guid.NewGuid(), name, rbacobject_id ?? RbacWellKnown.RootSecurityObjectId,
            created_at ?? now, updated_at ?? now, created_by ?? Guid.Empty, updated_by ?? Guid.Empty)
        {
            requested_operations = Normalize(operations)
        };
    }

    private static string[] Normalize(IEnumerable<string> values) => values
        .Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}

public sealed record RoleTypeUpdateRequest(
    Guid id,
    string name,
    string[] operations,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<RoleTypeModel>
{
    public static RoleTypeUpdateRequest FromModel(RoleTypeModel model)
        => new(model.id, model.name,
            model.operation_links.Where(x => x.operation is not null).Select(x => x.operation!.name).ToArray(),
            model.updated_at, model.updated_by);

    public RoleTypeModel ApplyTo(RoleTypeModel model) => model with
    {
        name = name,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by,
        requested_operations = operations.Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
    };

    public RoleTypeModel ToModel() => new(id, name, RbacWellKnown.RootSecurityObjectId,
        DateTimeOffset.UtcNow, updated_at ?? DateTimeOffset.UtcNow, Guid.Empty, updated_by ?? Guid.Empty)
    {
        requested_operations = operations
    };
}
