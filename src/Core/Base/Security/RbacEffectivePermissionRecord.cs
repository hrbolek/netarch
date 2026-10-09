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

public sealed record RbacEffectivePermissionRecord(
    Guid role_id,
    Guid group_id,
    string? group_name,
    Guid user_id,
    string? user_name,
    string role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    IReadOnlyCollection<string> operations
);
