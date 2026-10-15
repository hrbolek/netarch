using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DigitalProcesses;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class EntityOrganizationTests
{
    [Theory]
    [InlineData("Models")]
    [InlineData("Services")]
    [InlineData("Resolvers")]
    public void SourceFoldersMatchEntityNamespaces(string layer)
    {
        var directory = Path.Combine(FindModuleDirectory(), "DigitalProcesses." + layer);
        var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(directory, file).Split(Path.DirectorySeparatorChar)
                .Any(part => part is "obj" or "bin"))
            .Where(file => Path.GetFileName(file) != "GlobalUsings.cs").ToArray();
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(directory, file);
            var parts = relative.Split(Path.DirectorySeparatorChar);
            Assert.True(parts.Length == 2, $"Expected Entity/Type.cs: {relative}");
            var source = File.ReadAllText(file);
            Assert.Contains($"namespace WebAppArchitecture.DigitalProcesses.{layer}.{parts[0]};", source);
            if (parts[0] is not ("Shared" or "Infrastructure"))
                Assert.True(File.Exists(Path.Combine(FindModuleDirectory(), "DigitalProcesses.Models", parts[0], parts[0] + "Model.cs")),
                    $"{layer}/{parts[0]} must be named after an entity.");
        }
        foreach (var obsolete in new[] { layer, "DatabaseModels", "Forms", "Workflow", "Runtime", "Process", "Security", "Integration", "Rtf" })
            Assert.False(Directory.Exists(Path.Combine(directory, obsolete)), $"Obsolete directory {layer}/{obsolete}");
    }

    [Fact]
    public void DomainOperationsBelongToTheirSecuredEntityServices()
    {
        Assert.Equal(typeof(FormVersionService), typeof(FormVersionService).GetMethod("UpsertTreeAsync")!.DeclaringType);
        Assert.Equal(typeof(SubmissionService), typeof(SubmissionService).GetMethod("UpdateValuesAsync")!.DeclaringType);
        Assert.Equal(typeof(FormVersionService), typeof(FormVersionService).GetMethod("TemplatePageAsync")!.DeclaringType);
        Assert.Equal(typeof(ProcessDefinitionVersionService),
            typeof(ProcessDefinitionVersionService).GetMethod("ApplicantMatrixAsync")!.DeclaringType);
        var services = typeof(FormVersionService).Assembly.GetTypes();
        Assert.DoesNotContain(services, type => type.Name is "BulkTreeService" or "IBulkTreeService" or
            "DigitalProcessesManagementService" or "IDigitalProcessesManagementService" or "DefaultVersionContent");
        Assert.Contains(typeof(FormDesignAiService).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(IFormVersionService));
    }

    [Fact]
    public void DatabaseModelsRetainTheirTablesKeysAndEntityOwnership()
    {
        using var context = new DigitalProcessesDbContext(
            new DbContextOptionsBuilder<DigitalProcessesDbContext>()
                .UseNpgsql("Host=localhost;Database=schema_validation_only;Username=unused;Password=unused").Options);
        var models = typeof(FormVersionModel).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IEntityDBModel).IsAssignableFrom(type)).ToArray();
        Assert.Equal(19, models.Length);
        foreach (var model in models)
        {
            var entity = model.Name[..^"Model".Length];
            Assert.Equal($"WebAppArchitecture.DigitalProcesses.Models.{entity}", model.Namespace);
            var mapping = context.Model.FindEntityType(model);
            Assert.NotNull(mapping);
            Assert.StartsWith("dp_", mapping.GetTableName());
            Assert.Equal(["id"], mapping.FindPrimaryKey()!.Properties.Select(property => property.Name));
        }
        // SQL generation constructs the relational model without connecting to a database.
        var sql = context.Database.GenerateCreateScript();
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sql)));
        Assert.Equal("18D2C43D7D88274A710450978D374181450D49B2A8DE257AF97C461ACDAA4CA5", fingerprint);
    }

    private static string FindModuleDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var module = Path.Combine(directory.FullName, "src", "Apps", "DigitalProcesses");
            if (Directory.Exists(Path.Combine(module, "DigitalProcesses.Models")))
                return module;
        }
        throw new DirectoryNotFoundException("Cannot locate DigitalProcesses source files.");
    }
}
