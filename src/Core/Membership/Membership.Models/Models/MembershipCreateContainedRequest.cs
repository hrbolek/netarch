using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Models;

public sealed record MembershipCreateContainedRequest(
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null);
