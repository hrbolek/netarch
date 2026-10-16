using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text.Json;
using Architecture.Auditor;

if (args.Length < 1 || args.Length > 2)
{
    Console.Error.WriteLine("Usage: Architecture.Auditor <solution.slnx|solution.sln|project.csproj> [report.json]");
    return 2;
}
MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create();
workspace.WorkspaceFailed += (_, e) => Console.Error.WriteLine(e.Diagnostic.Message);
var path = Path.GetFullPath(args[0]);
var projects = path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
    ? new[] { await workspace.OpenProjectAsync(path) }
    : (await workspace.OpenSolutionAsync(path)).Projects.ToArray();
var findings = new List<Finding>();
foreach (var project in projects)
{
    var compilation = await project.GetCompilationAsync();
    if (compilation != null) findings.AddRange(Audit.Inspect(compilation));
}
var json = JsonSerializer.Serialize(findings, new JsonSerializerOptions { WriteIndented = true });
if (args.Length == 2) await File.WriteAllTextAsync(args[1], json);
else Console.WriteLine(json);
Console.Error.WriteLine($"Checked {projects.Length} projects, found {findings.Count} findings ({findings.Count(x => x.Severity == "error")} errors).");
return findings.Any(f => f.Severity == "error") ? 1 : 0;
