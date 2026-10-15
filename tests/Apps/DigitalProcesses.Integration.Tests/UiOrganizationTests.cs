using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.DigitalProcesses.UI.FormNode.Renderers;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class UiOrganizationTests
{
    [Fact]
    public void RazorComponentsFollowEntityFoldersAndNamespaces()
    {
        var ui = FindUiDirectory();
        var files = Directory.GetFiles(ui, "*.razor", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(ui, path).Split(Path.DirectorySeparatorChar)
                .Any(part => part is "bin" or "obj"))
            .Where(path => Path.GetFileName(path) != "_Imports.razor")
            .ToArray();
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(ui, file);
            var parts = relative.Split(Path.DirectorySeparatorChar);
            Assert.True(parts.Length == 3, $"Expected Entity/Role/Component.razor: {relative}");
            Assert.Contains(parts[1], new[] { "Pages", "Components", "Renderers" });
            var source = File.ReadAllText(file);
            Assert.Contains($"@namespace WebAppArchitecture.DigitalProcesses.UI.{parts[0]}.{parts[1]}", source);
            if (parts[1] == "Pages")
                Assert.Matches(@"<(?:Page|PageView|PageViewList)\b", source);
            else
                Assert.DoesNotMatch(@"<(?:Page|PageView|PageViewList)\b", source);
            if (parts[1] == "Renderers")
                Assert.Equal("FormNode", parts[0]);
            if (parts[1] == "Components")
            {
                Assert.DoesNotContain("System.Reflection", source);
                Assert.DoesNotContain("GetProperties(", source);
                Assert.DoesNotContain("switch (item)", source);
                Assert.DoesNotContain("Model is ", source);
            }
        }
        foreach (var oldFolder in new[] { "Pages", "Components", "Designer", "Renderers", "Routing", "Shared/Pages" })
            Assert.False(Directory.Exists(Path.Combine(ui, oldFolder)), $"Obsolete UI directory: {oldFolder}");
        var shared = Directory.GetFiles(Path.Combine(ui, "Shared", "Components"))
            .Select(Path.GetFileName).OrderBy(name => name).ToArray();
        Assert.Equal(new[]
        {
            "CreateFormField.razor",
            "CreateFormFields.cs",
            "DefinitionVersionItem.cs",
            "DefinitionVersionsTable.razor",
            "DigitalProcessCreateField.cs",
            "DigitalProcessCreatePageModel.cs",
            "DigitalProcessSelectOption.cs"
        }, shared);
    }

    [Fact]
    public void AllBuiltInRenderersRemainRegisteredInFormNodeNamespace()
    {
        using var services = new ServiceCollection().AddDigitalProcessRenderers().BuildServiceProvider();
        var registry = services.GetRequiredService<IFormNodeRendererRegistry>();
        var expected = new Dictionary<string, Type>
        {
            ["section"] = typeof(SectionRenderer),
            ["input.text"] = typeof(TextInputRenderer),
            ["input.textarea"] = typeof(TextAreaRenderer),
            ["input.number"] = typeof(NumberInputRenderer),
            ["input.date"] = typeof(DateInputRenderer),
            ["input.time"] = typeof(TimeInputRenderer),
            ["input.checkbox"] = typeof(CheckboxRenderer),
            ["input.select"] = typeof(SelectRenderer),
            ["input.graphql-search"] = typeof(GraphqlSearchRenderer),
            ["display.url"] = typeof(ComputedUrlRenderer),
            ["action.button"] = typeof(ActionButtonRenderer),
            ["action.import"] = typeof(ExternalDataImportNodeRenderer),
            ["action.document"] = typeof(DocumentActionNodeRenderer),
            ["action.export"] = typeof(ExternalDataExportNodeRenderer)
        };
        foreach (var (key, component) in expected)
        {
            Assert.Equal(component, registry.Resolve(key));
            Assert.Equal("WebAppArchitecture.DigitalProcesses.UI.FormNode.Renderers", component.Namespace);
            Assert.True(typeof(IComponent).IsAssignableFrom(component));
        }
    }

    private static string FindUiDirectory()
    {
        foreach (var origin in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(origin); directory is not null; directory = directory.Parent)
            {
                var ui = Path.Combine(directory.FullName, "src", "Apps", "DigitalProcesses", "DigitalProcesses.UI");
                if (File.Exists(Path.Combine(ui, "DigitalProcesses.UI.csproj")))
                    return ui;
            }
        }
        throw new DirectoryNotFoundException("Cannot locate DigitalProcesses.UI for source layout validation.");
    }
}
