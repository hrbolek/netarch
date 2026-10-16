using Architecture.Auditor;
using Architecture.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public class AuditTests
{
    private static IReadOnlyList<Finding> Check(string source)
    {
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p)).ToList();
        refs.Add(MetadataReference.CreateFromFile(typeof(ArchitectureAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create("Sample", [CSharpSyntaxTree.ParseText(source)], refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.DoesNotContain(compilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        return Audit.Inspect(compilation);
    }
    [Fact]
    public void CorrectTypesHaveNoFindings() => Assert.Empty(Check("""
        using Architecture.Attributes;
        [Architecture(ArchitectureLayer.Database, ArchitectureRole.Entity)] public class UserModel {}
        [Architecture(ArchitectureLayer.Model, ArchitectureRole.Response)] public class UserResponse {}
        [Architecture(ArchitectureLayer.Resolver, ArchitectureRole.Resolver)] public static class UserResolvers {
            public static UserResponse Get() => new();
        }
        """));

    [Fact]
    public void ResolverExposingEntityIsAnError() => Assert.Contains(Check("""
        using Architecture.Attributes;
        [Architecture(ArchitectureLayer.Database, ArchitectureRole.Entity)] public class UserModel {}
        [Architecture(ArchitectureLayer.Resolver, ArchitectureRole.Resolver)] public static class UserResolvers {
            public static System.Threading.Tasks.Task<UserModel> Get() => throw new System.Exception();
        }
        """), f => f.Rule == "ARCH101" && f.Severity == "error");

    [Fact]
    public void MissingAttributeIsAWarning() => Assert.Contains(Check("public class UserModel {}"), f => f.Rule == "ARCH001");
}
