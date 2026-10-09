using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

public sealed record GroupCreateContainedRequest(
    string Name,
    Guid master_group_id,
    Guid? group_type_id = null,
    bool inherit_roles = true);

/// <summary>Creates a root group. Authorization is evaluated on group_type_id.</summary>
