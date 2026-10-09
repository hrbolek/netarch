using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

public sealed record AssignRoleRequest(
    Guid group_id,
    Guid user_id,
    string role_type,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    bool is_deputy = false
);
