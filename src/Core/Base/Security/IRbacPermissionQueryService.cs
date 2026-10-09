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

public interface IRbacPermissionQueryService
{
    Task<IReadOnlyCollection<RbacEffectivePermissionRecord>> GetEffectivePermissionsAsync(
        Guid rbacObjectId,
        CancellationToken cancellationToken = default);
}
