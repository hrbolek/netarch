using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.Shared;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Events;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;
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
    public void DomainResolvers_InjectSecuredOrComposedServices_NotPersistenceOrAuthorizationLayer()
    {
        using var host = new IsolatedHost();
        using var scope = host.Services.CreateScope();
        var assemblies = new[]
        {
            typeof(FormVersionResolvers).Assembly,
            typeof(EventsResolvers).Assembly,
            typeof(WebAppArchitecture.User.Resolvers.UserResolvers).Assembly,
            typeof(WebAppArchitecture.Group.Resolvers.GroupResolvers).Assembly,
            typeof(WebAppArchitecture.GroupType.Resolvers.GroupTypeEndpointExtensions).Assembly,
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
            Assert.False(dependency == typeof(IAuthorizationLayer) ||
                typeof(WebAppArchitecture.DBLayer.IDBLayer).IsAssignableFrom(dependency) ||
                typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(dependency),
                $"{context} directly depends on persistence/security-layer type {dependency.Name}.");
            if (!dependency.Name.EndsWith("Service", StringComparison.Ordinal) ||
                dependency.ContainsGenericParameters ||
                dependency.Namespace == "WebAppArchitecture.Base.Security" ||
                dependency == typeof(IRbacAuthorizationService) ||
                dependency == typeof(WebAppArchitecture.Security.Services.ISecurityPermissionService))
                continue;
            var instance = scope.ServiceProvider.GetRequiredService(dependency);
            AssertSecuredOrComposed(instance.GetType(), scope.ServiceProvider, context);
        }
        AssertSecuredOrComposed(scope.ServiceProvider
            .GetRequiredService<IDigitalProcessActionService<FormVersionModel>>().GetType(),
            scope.ServiceProvider, "DigitalProcessActionService");
    }

    private static void AssertSecuredOrComposed(Type type, IServiceProvider services, string context)
    {
        if (IsSecured(type)) return;
        Assert.Equal(typeof(IDigitalProcessActionService<>).Assembly, type.Assembly);
        var dependencies = type.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        Assert.DoesNotContain(dependencies, dependency => dependency == typeof(IAuthorizationLayer) ||
            typeof(WebAppArchitecture.DBLayer.IDBLayer).IsAssignableFrom(dependency) ||
            typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(dependency));
        var entityContracts = dependencies.Where(dependency => dependency.GetInterfaces().Append(dependency)
            .Any(contract => contract.IsGenericType &&
                contract.GetGenericTypeDefinition() == typeof(WebAppArchitecture.Base.Services.ISecuredEntityService<>))).ToArray();
        Assert.True(entityContracts.Length > 0, $"{context} uses {type.Name} without a secured entity-service dependency.");
        foreach (var dependency in entityContracts)
            Assert.True(IsSecured(services.GetRequiredService(dependency).GetType()),
                $"{context} composes {dependency.Name}, which must resolve to a secured entity service.");
    }

    private static bool IsSecured(Type? type)
    {
        while (type is not null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SecuredEntityService<>))
                return true;
            type = type.BaseType;
        }
        return false;
    }
}
