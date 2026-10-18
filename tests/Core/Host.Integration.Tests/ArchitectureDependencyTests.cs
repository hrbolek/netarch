using System.Text.Json;
using Xunit;

namespace Core.Host.Integration.Tests;

public sealed class ArchitectureDependencyTests
{
    [Fact]
    public void GroupTypeContractsServicesAndResolversHaveTheirOwnAssemblies()
    {
        Assert.Equal("GroupType.Models", typeof(WebAppArchitecture.GroupType.Models.GroupTypeResponse).Assembly.GetName().Name);
        Assert.Equal("GroupType.Models", typeof(WebAppArchitecture.GroupType.Models.GroupTypeUpdateRequest).Assembly.GetName().Name);
        Assert.Equal("GroupType.Services", typeof(WebAppArchitecture.GroupType.Services.GroupTypeService).Assembly.GetName().Name);
        Assert.Equal("GroupType.Resolvers", typeof(WebAppArchitecture.GroupType.Resolvers.GroupTypeEndpointExtensions).Assembly.GetName().Name);
        foreach (var assembly in new[]
        {
            typeof(WebAppArchitecture.Group.Models.GroupResponse).Assembly,
            typeof(WebAppArchitecture.Group.Services.GroupService).Assembly,
            typeof(WebAppArchitecture.Group.Resolvers.GroupResolvers).Assembly
        })
            Assert.DoesNotContain(assembly.GetTypes(), type => type.Name.StartsWith("GroupType", StringComparison.Ordinal) ||
                type.Name == "IGroupTypeService");
    }

    [Fact]
    public void SecurityResponsibilitiesLiveInTheirOwningAssemblies()
    {
        var contracts = typeof(WebAppArchitecture.Base.Data.IEntityDBModel).Assembly;
        var baseAssembly = typeof(WebAppArchitecture.Base.Data.TreePath).Assembly;
        var hosting = typeof(WebAppArchitecture.Hosting.CoreModule).Assembly;
        var persistence = typeof(WebAppArchitecture.DBLayer.CoreDbContext).Assembly;
        Assert.Same(contracts, typeof(WebAppArchitecture.Base.Security.IRbacAuthorizationService).Assembly);
        Assert.Same(baseAssembly, typeof(WebAppArchitecture.Base.Security.ICurrentActorProvider).Assembly);
        Assert.Same(hosting, typeof(WebAppArchitecture.Base.Security.HeaderOrClaimActorProvider).Assembly);
        Assert.Same(hosting, typeof(WebAppArchitecture.Base.Security.EasyEntraHeaderPrincipalExtensions).Assembly);
        Assert.Same(hosting, typeof(WebAppArchitecture.Base.Security.EntraEndpointSecurity).Assembly);
        Assert.Same(persistence, typeof(WebAppArchitecture.Base.Security.FakeEntraSeedUsers).Assembly);
        Assert.Equal("AuthorizationLayer", typeof(WebAppArchitecture.AuthorizationLayer.IAuthorizationLayer).Assembly.GetName().Name);
        Assert.DoesNotContain(baseAssembly.GetReferencedAssemblies(), assembly =>
            assembly.Name is "Core.Hosting" or "AuthorizationLayer" or "DBLayer");
        Assert.All(new[] { "RbacEndpointExtensions", "RbacEndpointResolvers", "IRbacAdminService", "IRbacPermissionQueryService" },
            name => Assert.Null(baseAssembly.GetType("WebAppArchitecture.Base.Security." + name)));
    }

    [Fact]
    public void BaseForwardsExtractedContractsWithoutDefiningDuplicates()
    {
        var implementation = typeof(WebAppArchitecture.Base.Data.TreePath).Assembly;
        var contract = typeof(WebAppArchitecture.Base.Data.IEntityDBModel);
        Assert.Equal("Core.Contracts", contract.Assembly.GetName().Name);
        Assert.Contains(contract, implementation.GetForwardedTypes());
        Assert.Same(contract, implementation.GetType(contract.FullName!, throwOnError: true));
        Assert.DoesNotContain(implementation.GetTypes(), type => type.FullName == contract.FullName);
        Assert.Equal(18, implementation.GetForwardedTypes().Count(type => type.Assembly == contract.Assembly));
    }

    [Fact]
    public void ApplicationGraphDoesNotContainRetiredArchitectureTooling()
    {
        var path = Path.ChangeExtension(typeof(ArchitectureDependencyTests).Assembly.Location, ".deps.json");
        using var dependencies = JsonDocument.Parse(File.ReadAllText(path));
        var libraries = dependencies.RootElement.GetProperty("libraries")
            .EnumerateObject()
            .Select(library => library.Name.Split('/')[0])
            .ToArray();

        Assert.Contains("Host", libraries);
        Assert.Contains("AuthorizationLayer", libraries);
        Assert.DoesNotContain("SecurityLayer", libraries);
        Assert.DoesNotContain("Architecture.Attributes", libraries);
        Assert.DoesNotContain("Architecture.Auditor", libraries);
    }
}
