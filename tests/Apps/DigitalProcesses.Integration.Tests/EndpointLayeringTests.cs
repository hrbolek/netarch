using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.DigitalProcesses.Resolvers;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Events;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class EndpointLayeringTests
{
    [Fact]
    public void ProductionRoutes_UseNamedResolverMethods()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var sources = host.Services.GetServices<EndpointDataSource>();
        var routes = sources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>().ToArray();
        Assert.NotEmpty(routes);
        foreach (var endpoint in routes)
        {
            var method = endpoint.Metadata.GetMetadata<MethodInfo>();
            Assert.True(method is not null, $"Missing handler metadata: {endpoint.RoutePattern.RawText}");
            var owner = method!.DeclaringType!;
            var name = owner.FullName ?? owner.Name;
            if (name.StartsWith("WebAppArchitecture.JsonataPlayground.", StringComparison.Ordinal) ||
                name.Contains("PreviewFormEndpoints", StringComparison.Ordinal))
                continue;
            Assert.True(owner.Name.EndsWith("Resolvers", StringComparison.Ordinal),
                $"{endpoint.RoutePattern.RawText} uses {name}.{method.Name}, not a resolver method.");
        }
    }

    [Fact]
    public void DomainResolvers_InjectSecuredServices_NotPersistenceOrSecurityLayer()
    {
        using var host = new IsolatedHost();
        using var scope = host.Services.CreateScope();
        var assemblies = new[]
        {
            typeof(FormVersionResolvers).Assembly,
            typeof(EventsResolvers).Assembly,
            typeof(WebAppArchitecture.User.Resolvers.UserResolvers).Assembly,
            typeof(WebAppArchitecture.Group.Resolvers.GroupResolvers).Assembly,
            typeof(WebAppArchitecture.Membership.Resolvers.MembershipResolvers).Assembly,
            typeof(WebAppArchitecture.Role.Resolvers.RoleResolvers).Assembly,
            typeof(WebAppArchitecture.RoleType.Resolvers.RoleTypeResolvers).Assembly,
            typeof(WebAppArchitecture.Security.Resolvers.SecurityResolvers).Assembly
        };
        foreach (var type in assemblies.SelectMany(x => x.GetTypes())
            .Where(x => x.Name.EndsWith("Resolvers", StringComparison.Ordinal)))
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        foreach (var parameter in method.GetParameters())
        {
            var dependency = parameter.ParameterType;
            var context = $"{type.Name}.{method.Name}({parameter.Name})";
            Assert.False(dependency == typeof(ISecurityLayer) ||
                typeof(WebAppArchitecture.DBLayer.IDBLayer).IsAssignableFrom(dependency) ||
                typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(dependency),
                $"{context} directly depends on persistence/security-layer type {dependency.Name}.");
            if (!dependency.Name.EndsWith("Service", StringComparison.Ordinal) ||
                dependency.ContainsGenericParameters ||
                dependency.Namespace == "WebAppArchitecture.Base.Security" ||
                dependency == typeof(ISecurityService) ||
                dependency == typeof(WebAppArchitecture.Security.Services.ISecurityPermissionService))
                continue;
            var instance = scope.ServiceProvider.GetRequiredService(dependency);
            Assert.True(IsSecured(instance.GetType()),
                $"{context} uses {instance.GetType().Name}, which does not derive from SecuredBaseService.");
        }
        Assert.True(IsSecured(scope.ServiceProvider
            .GetRequiredService<IDigitalProcessActionService<FormVersionModel>>().GetType()));
    }

    private static bool IsSecured(Type? type)
    {
        while (type is not null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SecuredBaseService<>))
                return true;
            type = type.BaseType;
        }
        return false;
    }
}
