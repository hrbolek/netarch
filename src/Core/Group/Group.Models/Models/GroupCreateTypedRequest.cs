using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

public sealed record GroupCreateTypedRequest(
    string Name,
    Guid group_type_id,
    bool inherit_roles = true);
