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

public static class RbacEndpointExtensions
{
    public static IEndpointRouteBuilder MapRbacEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix).WithTags("RBAC");
        group.MapGet("/health", RbacEndpointResolvers.Health)
            .WithDescription("""
                Returns the legacy RBAC subsystem health information and well-known bootstrap identifiers.
                """);
        group.MapGet("/access/can", RbacEndpointResolvers.CanAccess)
            .WithDescription("""
                Evaluates whether the selected user is allowed to perform an operation on an RBAC object.
                """);
        group.MapGet("/permissions/{rbacId:guid}", RbacEndpointResolvers.Permissions)
            .WithDescription("""
                Renders the legacy HTML permission list for an RBAC object after checking read access.
                """);
        return endpoints;
    }
}
