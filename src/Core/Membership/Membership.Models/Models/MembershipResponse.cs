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
