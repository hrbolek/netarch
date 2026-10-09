using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Models;

public sealed record UserCreateContainedRequest(
    string Name,
    Guid group_id,
    string? email = null,
    string? surname = null,
    DateTimeOffset? membership_valid_from = null,
    DateTimeOffset? membership_valid_to = null);
