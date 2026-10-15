using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using WebAppArchitecture.DigitalProcesses.UI.ProcessDefinition.Pages;
using WebAppArchitecture.DigitalProcesses.UI.WorkflowDefinition.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.Base.UI.Routing;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class DefinitionVersionsPageTests
{
    [Theory]
    [InlineData("Process")]
    [InlineData("Workflow")]
    public async Task EmptyDefinitionPreservesCreationAction(string kind)
    {
        var model = CreateModel(kind, []);
        var html = await Render(model);

        Assert.Contains($"/digital-processes/{kind}DefinitionResponse/list", html);
        Assert.Contains("No versions yet. Create a draft to begin.", html);
        if (kind == "Process")
        {
            Assert.Contains($"/digital-processes/ProcessDefinitionVersionResponse/create?processId={model.Id}", html);
            Assert.Contains("Create first version", html);
            Assert.DoesNotContain("Copy to new draft", html);
        }
        else
        {
            Assert.Contains($"/digital-processes/WorkflowDefinitionResponse/new-version/{model.Id}", html);
            Assert.Contains("Copy to new draft", html);
            Assert.DoesNotContain("Create first version", html);
        }
    }

    [Theory]
    [InlineData("Process")]
    [InlineData("Workflow")]
    public async Task VersionsPreserveOrderingAndKindSpecificActions(string kind)
    {
        var draft = new DefinitionVersionItem(Guid.NewGuid(), 1, false, DateTimeOffset.UtcNow);
        var published = new DefinitionVersionItem(Guid.NewGuid(), 2, true, DateTimeOffset.UtcNow);
        var model = CreateModel(kind, [draft, published]);
        var html = await Render(model);

        Assert.Contains("Version 1", html);
        Assert.Contains("Version 2", html);
        Assert.True(html.IndexOf("Version 2", StringComparison.Ordinal) < html.IndexOf("Version 1", StringComparison.Ordinal));
        Assert.Contains("Published", html);
        Assert.Contains("Draft", html);
        Assert.Contains($"/digital-processes/{kind}DefinitionResponse/new-version/{model.Id}", html);
        Assert.Contains($"/digital-processes/{kind}DefinitionResponse/publish-version/{draft.Id}?definitionId={model.Id}", html);
        Assert.Contains($"/digital-processes/{kind}DefinitionResponse/delete-version/{draft.Id}?definitionId={model.Id}", html);
        Assert.DoesNotContain($"publish-version/{published.Id}", html);
        Assert.DoesNotContain($"delete-version/{published.Id}", html);
        Assert.Contains("return confirm(&#x27;Delete draft version?&#x27;);", html);
        Assert.DoesNotContain("No versions yet.", html);

        foreach (var version in model.Versions)
        {
            if (kind == "Process")
            {
                Assert.Contains($"/digital-processes/ProcessDefinitionVersionResponse/view/{version.Id}", html);
                Assert.Contains($"/digital-processes/process-versions/{version.Id}/permissions", html);
                Assert.DoesNotContain("WorkflowVersionResponse/designer/", html);
            }
            else
            {
                Assert.Contains($"/digital-processes/WorkflowVersionResponse/designer/{version.Id}", html);
                Assert.Contains(version.Published ? "View workflow" : "Edit states and transitions", html);
                Assert.DoesNotContain("/permissions", html);
                Assert.DoesNotContain("ProcessDefinitionVersionResponse/view/", html);
            }
        }
    }

    [Theory]
    [InlineData("Process")]
    [InlineData("Workflow")]
    public void DomainModelsCarryOnlyTheirOwnDefinition(string kind)
    {
        var model = CreateModel(kind, []);
        var json = System.Text.Json.JsonSerializer.Serialize(model.Model, model.Model.GetType());
        Assert.Contains($"\"Kind\":\"{kind}\"", json);
        Assert.Contains($"\"{kind}Definition\":", json);
        Assert.DoesNotContain(kind == "Process" ? "\"WorkflowDefinition\":" : "\"ProcessDefinition\":", json);
    }

    [Theory]
    [InlineData("Process")]
    [InlineData("Workflow")]
    public async Task MissingDefinitionShowsError(string kind)
    {
        var html = await Render(CreateModel(kind, [], missingDefinition: true));

        Assert.Contains("Definition was not loaded.", html);
        Assert.DoesNotContain("<table", html);
    }

    private sealed record PageCase(Guid Id, IReadOnlyList<DefinitionVersionItem> Versions, Type Component, object Model);

    private static PageCase CreateModel(string kind, IReadOnlyList<DefinitionVersionItem> versions, bool missingDefinition = false)
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        return kind switch
        {
            "Process" => new(id, versions, typeof(DefinitionProcessVersionPage),
                new DefinitionProcessVersionPageModel(id, "Definition versions test", versions,
                    missingDefinition ? null : new ProcessDefinitionResponse(id, "Process", null, Guid.Empty, Guid.Empty, now, now, Guid.Empty, Guid.Empty))),
            "Workflow" => new(id, versions, typeof(DefinitionWorkflowVersionPage),
                new DefinitionWorkflowVersionPageModel(id, "Definition versions test", versions,
                    missingDefinition ? null : new WorkflowDefinitionResponse(id, "Workflow", null, Guid.Empty, now, now, Guid.Empty, Guid.Empty))),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static async Task<string> Render(PageCase model)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddHttpContextAccessor()
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .AddSingleton<IApplicationNavigationRegistry, ApplicationNavigationRegistry>()
            .AddSingleton<IEntityRouteRegistry, EntityRouteRegistry>()
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync(model.Component, ParameterView.FromDictionary(
                new Dictionary<string, object?> { ["Model"] = model.Model }))).ToHtmlString());
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/digital-processes");
    }
}
