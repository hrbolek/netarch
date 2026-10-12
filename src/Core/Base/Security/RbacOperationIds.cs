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

public static class RbacOperationIds
{
    public static readonly Guid Create = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid Read = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid Update = Guid.Parse("20000000-0000-0000-0000-000000000003");
    public static readonly Guid Delete = Guid.Parse("20000000-0000-0000-0000-000000000004");
    public static readonly Guid List = Guid.Parse("20000000-0000-0000-0000-000000000005");
    public static readonly Guid Vector = Guid.Parse("20000000-0000-0000-0000-000000000006");
    public static readonly Guid ManageGroups = Guid.Parse("20000000-0000-0000-0000-000000000007");
    public static readonly Guid ManageMemberships = Guid.Parse("20000000-0000-0000-0000-000000000008");
    public static readonly Guid ManageRoles = Guid.Parse("20000000-0000-0000-0000-000000000009");
    public static readonly Guid ManageSecurityObjects = Guid.Parse("20000000-0000-0000-0000-000000000010");

    public static readonly Guid StartProcess = Guid.Parse("20000000-0000-0000-0000-000000000011");

    public static Guid FromName(string operation) => operation.Trim().ToLowerInvariant() switch
    {
        RbacOperations.StartProcess => StartProcess,
        RbacOperations.Create => Create,
        RbacOperations.Read => Read,
        RbacOperations.Update => Update,
        RbacOperations.Delete => Delete,
        RbacOperations.List => List,
        RbacOperations.Vector => Vector,
        RbacOperations.ManageGroups => ManageGroups,
        RbacOperations.ManageMemberships => ManageMemberships,
        RbacOperations.ManageRoles => ManageRoles,
        RbacOperations.ManageSecurityObjects => ManageSecurityObjects,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown well-known RBAC operation.")
    };

    public static IReadOnlyDictionary<string, Guid> All { get; } =
        new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [RbacOperations.StartProcess] = StartProcess,
            [RbacOperations.Create] = Create,
            [RbacOperations.Read] = Read,
            [RbacOperations.Update] = Update,
            [RbacOperations.Delete] = Delete,
            [RbacOperations.List] = List,
            [RbacOperations.Vector] = Vector,
            [RbacOperations.ManageGroups] = ManageGroups,
            [RbacOperations.ManageMemberships] = ManageMemberships,
            [RbacOperations.ManageRoles] = ManageRoles,
            [RbacOperations.ManageSecurityObjects] = ManageSecurityObjects,
        };
}
