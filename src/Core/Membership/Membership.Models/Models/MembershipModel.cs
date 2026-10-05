using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Models;

public sealed record MembershipResponse(
    Guid id,
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string? group_name = null,
    string? user_name = null,
    string visual_label = ""
) : IEntityDTOModel<MembershipModel, MembershipResponse>, ISecuredEntityDTOModel
{
    public static MembershipResponse FromModel(MembershipModel model) => new(
        model.id, model.group_id, model.user_id, model.valid_from, model.valid_to, model.rbacobject_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by, model.group_name, model.user_name,
        (model.user_name ?? "User") + " @ " + (model.group_name ?? "Group"));

    public static Expression<Func<MembershipModel, MembershipResponse>> Projection => model => new MembershipResponse(
        model.id, model.group_id, model.user_id, model.valid_from, model.valid_to, model.rbacobject_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by, model.group_name, model.user_name,
        (model.user_name ?? "User") + " @ " + (model.group_name ?? "Group"));

    public MembershipModel ToModel() => new(id, group_id, user_id, valid_from, valid_to, rbacobject_id,
        created_at, updated_at, created_by, updated_by)
    {
        group_name = group_name,
        user_name = user_name
    };
}

public sealed record MembershipCreateRequest(
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    Guid? rbacobject_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<MembershipModel>, ISecuredCreateRequest
{
    public MembershipModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        return new(Guid.NewGuid(), group_id, user_id, valid_from, valid_to,
            rbacobject_id ?? RbacWellKnown.RootSecurityObjectId,
            created_at ?? now, updated_at ?? now, created_by ?? Guid.Empty, updated_by ?? Guid.Empty);
    }
}

public sealed record MembershipUpdateRequest(
    Guid id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<MembershipModel>
{
    public static MembershipUpdateRequest FromModel(MembershipModel model)
        => new(model.id, model.valid_from, model.valid_to, model.updated_at, model.updated_by);

    public MembershipModel ApplyTo(MembershipModel model) => model with
    {
        valid_from = valid_from,
        valid_to = valid_to,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    public MembershipModel ToModel() => new(id, Guid.Empty, Guid.Empty, valid_from, valid_to,
        RbacWellKnown.RootSecurityObjectId, DateTimeOffset.UtcNow,
        updated_at ?? DateTimeOffset.UtcNow, Guid.Empty, updated_by ?? Guid.Empty);
}
