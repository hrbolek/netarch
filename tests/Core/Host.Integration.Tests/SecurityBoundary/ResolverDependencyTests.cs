using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.DBLayer;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class ResolverDependencyTests
{
    // These existing assembly references expose model types/operation constants.
    // They do not exempt persistence members from the compiled semantic checks.
    private static readonly IReadOnlySet<(string Project, string Reference)> ContractReferences =
        new HashSet<(string, string)>
        {
            ("src/Core/GroupType/GroupType.Resolvers/GroupType.Resolvers.csproj", "src/Core/DBLayer/DBLayer.csproj"),
            ("src/Core/Security/Security.Resolvers/Security.Resolvers.csproj", "src/Core/AuthorizationLayer/AuthorizationLayer.csproj"),
            ("src/Apps/Events/Events.Resolvers/Events.Resolvers.csproj", "src/Core/DBLayer/DBLayer.csproj"),
            ("src/Apps/Events/Events.Resolvers/Events.Resolvers.csproj", "src/Core/AuthorizationLayer/AuthorizationLayer.csproj"),
            ("src/Apps/DigitalProcesses/DigitalProcesses.Resolvers/DigitalProcesses.Resolvers.csproj", "src/Core/DBLayer/DBLayer.csproj")
        };

    [Fact]
    public void ResolverProjectReferencesCannotAddPersistenceDependencies()
    {
        var root = RepositoryRoot();
        var observed = new HashSet<(string, string)>();
        foreach (var project in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.Resolvers.csproj", SearchOption.AllDirectories))
        {
            var document = XDocument.Load(project);
            foreach (var reference in document.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value ?? throw new InvalidOperationException("Missing ProjectReference Include");
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, include.Replace('\\', '/')));
                Assert.True(File.Exists(target), $"Unresolved project reference: {project} -> {include}");
                var targetName = Path.GetFileNameWithoutExtension(target);
                if (targetName is not ("DBLayer" or "AuthorizationLayer" or "AppComposition" or "Host") &&
                    !targetName.EndsWith(".DBLayer", StringComparison.Ordinal))
                    continue;
                var pair = (Relative(root, project), Relative(root, target));
                observed.Add(pair);
                Assert.True(ContractReferences.Contains(pair), $"ZA-LAYER-001: forbidden project reference {pair}");
            }
        }
        Assert.True(ContractReferences.SetEquals(observed),
            "Remove stale model/constant reference exceptions when project references change.");
    }

    [Fact]
    public void CompiledResolversCannotAccessPersistenceEvenThroughServiceLocationOrAsyncHelpers()
    {
        using var host = new ResolverBoundaryHost();
        var handlers = host.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.Metadata.GetRequiredMetadata<MethodInfo>().DeclaringType
                ?? throw new InvalidOperationException("Registered handler has no declaring type")).ToArray();
        var resolverAssemblies = handlers.Select(type => type.Assembly)
            .Where(assembly => assembly.GetName().Name?.EndsWith(".Resolvers", StringComparison.Ordinal) == true)
            .Distinct().ToArray();
        Assert.NotEmpty(resolverAssemblies);
        var types = resolverAssemblies.SelectMany(assembly => assembly.GetTypes())
            .Concat(handlers.SelectMany(HandlerFamily))
            .Concat(typeof(BaseResolvers).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            .Append(typeof(BaseResolvers))
            .Distinct();
        var violations = types.SelectMany(CompiledBoundaryInspector.Inspect).Distinct().ToArray();
        Assert.True(violations.Length == 0, "ZA-AUTH-001 / ZA-LAYER-001:\n" + string.Join("\n", violations));
        host.Verify();
    }

    [Fact]
    public void InspectorDetectsCompiledPersistenceCallsAndCapturedDependencies()
    {
        var violations = CompiledBoundaryInspector.Inspect(typeof(ForbiddenExample)).ToArray();
        Assert.Contains(violations, violation => violation.Contains("field of persistence type", StringComparison.Ordinal));
        Assert.Contains(violations, violation => violation.Contains("compiled reference", StringComparison.Ordinal));
        Assert.Contains(violations, violation => violation.Contains(".Located: compiled reference", StringComparison.Ordinal));
        Assert.Contains(violations, violation => violation.Contains(".OpenRawConnection: compiled reference", StringComparison.Ordinal));
    }

    private sealed class ForbiddenExample(IDBLayer db)
    {
        public IQueryable<UserModel> Read() => db.Query<UserModel>();
        public object Located(IServiceProvider services) => services.GetRequiredService<CoreDbContext>();
        public void OpenRawConnection()
        {
            using var connection = new Npgsql.NpgsqlConnection();
            connection.Open();
        }
    }

    private static IEnumerable<Type> HandlerFamily(Type type)
        => type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(HandlerFamily).Prepend(type);

    internal static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SignFakeArchitecture.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Cannot find solution root for project-reference inspection.");
    }

    private static string Relative(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');
}
