using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Models;

public sealed record RoleCreateContainedRequest(
    Guid group_id,
    Guid user_id,
    Guid role_type_id,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    Guid? delegated_by_role_id = null);
