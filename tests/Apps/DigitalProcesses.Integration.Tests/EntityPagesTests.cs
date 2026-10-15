using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.DigitalProcesses.UI.FormDefinition.Pages;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class EntityPagesTests
{
    [Fact]
    public async Task AdministrationLinksToExistingCreationScopeRoleManagement()
    {
        var registry = new ApplicationNavigationRegistry();
        WebAppArchitecture.DigitalProcesses.UI.Shared.Routing.DigitalProcessNavigationRegistration
            .RegisterDigitalProcesses(registry);
        var application = registry.Get("digital-processes");
        Assert.NotNull(application);
        var administration = Assert.Single(application.Navigation, item => item.Label == "Administration");
        Assert.NotNull(administration.Children);
        Assert.Contains(administration.Children, item =>
            item.Label == "Creation permissions and roles" && item.Url == "/architecture/root-permissions");

        var html = await Render(
            typeof(WebAppArchitecture.Security.UI.Pages.RootPermissionAdministrationPage),
            new()
            {
                ["Model"] = new WebAppArchitecture.Security.Models.RbacPermissionResponse(
                    WebAppArchitecture.Base.Security.RbacWellKnown.RootGroupId, []),
                ["IsFragment"] = false
            });
        foreach (var scope in new[]
        {
            WebAppArchitecture.Base.Security.RbacWellKnown.RootGroupId,
            WebAppArchitecture.Base.Security.RbacWellKnown.FormDefinitionCreationScopeId,
            WebAppArchitecture.Base.Security.RbacWellKnown.WorkflowDefinitionCreationScopeId,
            WebAppArchitecture.Base.Security.RbacWellKnown.ProcessDefinitionCreationScopeId,
            WebAppArchitecture.Base.Security.RbacWellKnown.FormNodeTypeCreationScopeId
        })
        {
            Assert.Contains($"/architecture/GroupResponse/view/{scope}", html);
            Assert.Contains($"/architecture/RbacPermissionResponse/permissions/{scope}", html);
            Assert.Contains($"/architecture/RbacPermissionResponse/create-role-action/{scope}", html);
        }
        Assert.Contains("Create Form Definitions", html);
        Assert.Contains("rbacobject_id", html);
    }

    private static readonly IReadOnlyDictionary<string, string> ListTitles = new Dictionary<string, string>
    {
        ["FormDefinition"] = "Form definitions",
        ["FormVersion"] = "Form versions",
        ["FormNode"] = "Form nodes",
        ["FormNodeType"] = "Form node types",
        ["ProcessDefinition"] = "Process definitions",
        ["ProcessDefinitionVersion"] = "Process definition versions",
        ["WorkflowDefinition"] = "Workflow definitions",
        ["WorkflowVersion"] = "Workflow versions",
        ["WorkflowState"] = "Workflow states",
        ["WorkflowTransition"] = "Workflow transitions",
        ["WorkflowStateAccessRule"] = "Workflow state access rules",
        ["Request"] = "Requests",
        ["RequestTransitionHistory"] = "Request transition history",
        ["Submission"] = "Submissions",
        ["SubmissionNode"] = "Submission nodes"
    };

    [Fact]
    public async Task EveryEntityListPreservesTitleAndOptionalFilter()
    {
        foreach (var (entity, title) in ListTitles)
        {
            var page = typeof(FormDefinitionListPage).Assembly.GetType(
                $"WebAppArchitecture.DigitalProcesses.UI.{entity}.Pages.{entity}ListPage", throwOnError: true)!;
            var modelType = page.GetProperty("Model")!.PropertyType.GetElementType()!;
            Assert.Equal(entity + "Response", modelType.Name);
            var model = Array.CreateInstance(modelType, 0);
            foreach (var showFilter in new[] { true, false })
            {
                var html = await Render(page, new() { ["Model"] = model, ["ShowFilter"] = showFilter });
                Assert.Contains($"<title>{title}</title>", html);
                Assert.Contains("No items.", html);
                Assert.Equal(showFilter, html.Contains("id=\"where-filter\"", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public async Task EveryEntityCreatePagePreservesFormConfiguration()
    {
        var pages = typeof(FormDefinitionCreatePage).Assembly.GetTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type) &&
                type.Name.EndsWith("CreatePage", StringComparison.Ordinal) &&
                type.Namespace?.StartsWith("WebAppArchitecture.DigitalProcesses.UI.", StringComparison.Ordinal) == true)
            .ToArray();
        Assert.Equal(12, pages.Length);
        foreach (var page in pages)
        {
            var entity = page.Name[..^"CreatePage".Length];
            Assert.Equal($"WebAppArchitecture.DigitalProcesses.UI.{entity}.Pages", page.Namespace);
            var action = $"/digital-processes/{entity}Response/create-form";
            var listUrl = $"/digital-processes/{entity}Response/list";
            var configuration = page.Assembly.GetType(
                $"WebAppArchitecture.DigitalProcesses.UI.{entity}.Components.{entity}CreateForm", throwOnError: true)!;
            var model = (DigitalProcessCreatePageModel)configuration.GetMethod("CreatePage")!.Invoke(null, null)!;
            var html = await Render(page, new() { ["Model"] = model });
            Assert.Contains($"<title>{System.Net.WebUtility.HtmlEncode(model.Title)}</title>", html);
            Assert.Contains($"action=\"{action}\"", html);
            Assert.Contains($"href=\"{listUrl}\"", html);
            foreach (var field in model.Fields)
            {
                Assert.Contains($"name=\"{field.Name}\"", html);
                if (field.InputType == "checkbox")
                    Assert.Contains($"type=\"checkbox\" name=\"{field.Name}\" value=\"true\"", html);
                if (field.DefaultValue is not null)
                    Assert.Contains($"value=\"{field.DefaultValue}\"", html);
                if (field.Min is not null)
                    Assert.Contains($"min=\"{field.Min}\"", html);
                if (field.Max is not null)
                    Assert.Contains($"max=\"{field.Max}\"", html);
                if (field.ParentField is not null)
                    Assert.Contains($"data-parent-field=\"{field.ParentField}\"", html);
                if (field.ParentProperty is not null)
                    Assert.Contains($"data-parent-property=\"{field.ParentProperty}\"", html);
            }
            Assert.Contains(model.Description!, System.Net.WebUtility.HtmlDecode(html));
            Assert.Contains(model.SubmitLabel, html);
            Assert.Equal(
                $"WebAppArchitecture.DigitalProcesses.UI.{entity}.Components",
                page.Assembly.GetType($"WebAppArchitecture.DigitalProcesses.UI.{entity}.Components.{entity}CreateCard", true)!.Namespace);
        }
    }

    [Fact]
    public void EveryEntityModelPageHasItsOwnDtoContract()
    {
        var pages = typeof(FormDefinitionModelPage).Assembly.GetTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type) &&
                type.Name.EndsWith("ModelPage", StringComparison.Ordinal) &&
                type.Namespace?.StartsWith("WebAppArchitecture.DigitalProcesses.UI.", StringComparison.Ordinal) == true)
            .ToArray();
        Assert.Equal(15, pages.Length);
        foreach (var page in pages)
        {
            var entity = page.Name[..^"ModelPage".Length];
            Assert.False(page.IsGenericType);
            Assert.Equal($"WebAppArchitecture.DigitalProcesses.UI.{entity}.Pages", page.Namespace);
            Assert.Equal(entity + "Response", page.GetProperty("Model")!.PropertyType.Name);
        }
    }

    private static async Task<string> Render(Type component, Dictionary<string, object?> parameters)
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
            (await renderer.RenderComponentAsync(component, ParameterView.FromDictionary(parameters))).ToHtmlString());
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/digital-processes");
    }
}
