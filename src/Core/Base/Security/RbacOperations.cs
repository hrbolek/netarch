using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

public static class RbacOperations
{
    public const string Create = "create";
    public const string Read = "read";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string List = "list";
    public const string Vector = "vector";
    public const string ManageGroups = "manage_groups";
    public const string ManageMemberships = "manage_memberships";
    public const string ManageRoles = "manage_roles";
    public const string ManageSecurityObjects = "manage_security_objects";
    public const string StartProcess = "start_process";
}
