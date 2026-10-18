using System.Xml.Linq;
using Xunit;

namespace Core.Host.Integration.Tests;

public sealed class CoreLayoutTests
{
    [Fact]
    public void CoreSourcesFollowOwnershipAndLayerConventions()
    {
        var root = RepositoryRoot();
        Assert.True(File.Exists(Path.Combine(root, "src/Core/Base/UI/Pages/CreatePageModel.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/Core/Base/Base.UI")));
        Assert.True(File.Exists(Path.Combine(root, "src/Core/Contracts/Core.Contracts.csproj")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/Core/Core.Contracts")));

        foreach (var entity in new[] { "Group", "GroupType", "Role", "RoleType", "Membership", "User", "Security" })
        foreach (var layer in new[] { "Models", "Services", "Resolvers" })
        {
            var project = Path.Combine(root, $"src/Core/{entity}/{entity}.{layer}");
            Assert.True(File.Exists(Path.Combine(project, $"{entity}.{layer}.csproj")));
            Assert.False(Directory.Exists(Path.Combine(project, layer)));
        }
        Assert.False(Directory.Exists(Path.Combine(root, "src/Core/GroupType/GroupType.UI")));

        foreach (var entity in new[] { "Group", "GroupType", "Role", "RoleType", "Membership", "User" })
        foreach (var name in new[] { $"{entity}Model", $"I{entity}Store", $"Ef{entity}Store" })
        {
            Assert.True(File.Exists(Path.Combine(root, $"src/Core/DBLayer/Entities/{entity}/{name}.cs")));
            Assert.False(File.Exists(Path.Combine(root, $"src/Core/DBLayer/{name}.cs")));
        }
        foreach (var name in new[] { "EffectiveAccessModel", "RbacAccessBindingModel", "RbacDenyRuleModel", "OperationModel", "RoleTypeOperationModel" })
            Assert.True(File.Exists(Path.Combine(root, $"src/Core/DBLayer/Authorization/{name}.cs")));
        foreach (var name in new[] { "DatabaseInitializer", "SystemDataImporter", "SystemDataImportResult" })
            Assert.True(File.Exists(Path.Combine(root, $"src/Core/DBLayer/Seeding/{name}.cs")));

        foreach (var (template, layer, file) in new[]
        {
            ("EntityModelTemplate", "Models", "EntityNameModel.cs"),
            ("EntityServiceTemplate", "Services", "EntityNameService.cs"),
            ("EntityResolversTemplate", "Resolvers", "EntityNameResolvers.cs")
        })
        {
            Assert.True(File.Exists(Path.Combine(root, $"templates/{template}/{file}")));
            Assert.False(Directory.Exists(Path.Combine(root, $"templates/{template}/{layer}")));
        }
    }

    [Fact]
    public void ProjectReferencesExistAndDoNotFormCycles()
    {
        var root = RepositoryRoot();
        var projects = new[] { "src", "tests", "tools" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.csproj", SearchOption.AllDirectories))
            .ToDictionary(Path.GetFullPath, project => XDocument.Load(project)
                .Descendants("ProjectReference")
                .Select(reference => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!,
                    reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))))
                .ToArray());
        var visited = new HashSet<string>();
        var active = new HashSet<string>();
        foreach (var project in projects.Keys)
            Visit(project);

        var contracts = Path.Combine(root, "src/Core/Contracts/Core.Contracts.csproj");
        Assert.Empty(projects[contracts]);
        Assert.Empty(XDocument.Load(contracts).Descendants("PackageReference"));

        void Visit(string project)
        {
            Assert.True(File.Exists(project), $"Missing project reference target: {project}");
            Assert.DoesNotContain(project, active);
            if (!visited.Add(project)) return;
            active.Add(project);
            foreach (var dependency in projects[project])
                Visit(dependency);
            active.Remove(project);
        }
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SignFakeArchitecture.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Core layout tests require the repository checkout.");
    }
}
