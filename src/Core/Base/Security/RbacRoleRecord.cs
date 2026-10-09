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

public sealed record RbacRoleRecord(
    Guid id,
    Guid group_id,
    Guid user_id,
    string role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid? delegated_by_role_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
);
