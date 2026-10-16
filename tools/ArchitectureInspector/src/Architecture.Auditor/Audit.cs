using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Architecture.Auditor;

public sealed record Finding(string Rule, string Severity, string Type, string Message, string? File, int? Line);

public static class Audit
{
    private const string AttributeName = "Architecture.Attributes.ArchitectureAttribute";
    public static IReadOnlyList<Finding> Inspect(Compilation compilation)
    {
        var findings = new List<Finding>();
        foreach (var type in Types(compilation.Assembly.GlobalNamespace))
        {
            if (type.IsImplicitlyDeclared) continue;
            var label = type.ToDisplayString();
            var role = GetRole(type);
            var location = type.Locations.FirstOrDefault(l => l.IsInSource);
            void Add(string rule, string severity, string message)
            {
                var span = location?.GetLineSpan();
                findings.Add(new(rule, severity, label, message, span?.Path, span?.StartLinePosition.Line + 1));
            }
            var expected = ExpectedRole(type.Name);
            if (expected != null && role == null)
                Add("ARCH001", "warning", $"Conventional type lacks [Architecture] ({expected}).");
            if (role == null) continue;
            if (expected != null && role != expected)
                Add("ARCH002", "error", $"Annotated role {role} conflicts with conventional role {expected}.");
            if (role != "Resolver") continue;
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary))
            {
                if (ContainsDatabaseEntity(method.ReturnType))
                    Add("ARCH101", "error", $"Resolver method {method.Name} exposes a database entity in its return type.");
                foreach (var parameter in method.Parameters)
                    if (ContainsDatabaseEntity(parameter.Type))
                        Add("ARCH102", "error", $"Resolver method {method.Name} accepts database entity parameter {parameter.Name}.");
            }
        }
        return findings;
    }
    private static string? ExpectedRole(string name) => name switch
    {
        _ when name.EndsWith("Model", StringComparison.Ordinal) => "Entity",
        _ when name.EndsWith("Response", StringComparison.Ordinal) => "Response",
        _ when name.EndsWith("Service", StringComparison.Ordinal) && !name.StartsWith('I') => "Service",
        _ when name.EndsWith("Resolvers", StringComparison.Ordinal) => "Resolver",
        _ => null
    };
    private static string? GetRole(INamedTypeSymbol type)
    {
        var a = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttributeName);
        return a is { ConstructorArguments.Length: 2 } && a.ConstructorArguments[1].Value is int value
            ? Enum.GetName(typeof(Architecture.Attributes.ArchitectureRole), value)
            : null;
    }
    private static bool ContainsDatabaseEntity(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array) return ContainsDatabaseEntity(array.ElementType);
        if (type is not INamedTypeSymbol named) return false;
        if (GetRole(named) == "Entity") return true;
        return named.TypeArguments.Any(ContainsDatabaseEntity);
    }
    private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol ns)
    {
        foreach (var child in ns.GetNamespaceMembers())
            foreach (var type in Types(child)) yield return type;
        foreach (var type in ns.GetTypeMembers())
            foreach (var nested in Types(type)) yield return nested;
    }
    private static IEnumerable<INamedTypeSymbol> Types(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers())
            foreach (var item in Types(nested)) yield return item;
    }
}
