using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.UI.FormNode.Components;
using WebAppArchitecture.DigitalProcesses.UI.FormNode.Renderers;
using WebAppArchitecture.DigitalProcesses.UI.FormVersion.Pages;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormVersionDesignerPageTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public async Task DesignerPreservesDraftPublishedPreviewAndSubtreeModes(bool published, bool preview, bool subtree)
    {
        var model = Model(published, preview, subtree);
        var html = await Render(model);
        var decoded = WebUtility.HtmlDecode(html);
        Assert.Contains("<title>Form designer</title>", html);
        Assert.Contains("Oprávnění verze formuláře", decoded);
        var editor = new FormNodeEditorView(model);
        Assert.Contains($"href=\"{editor.DesignerUrl}\"", html);
        Assert.Contains($"href=\"{editor.TestUrl}\"", html);
        var editing = !published && !preview;
        Assert.Equal(editing, html.Contains("<strong>Structure</strong>", StringComparison.Ordinal));
        Assert.Equal(editing, html.Contains("id=\"edit-node-", StringComparison.Ordinal));
        Assert.Equal(editing, html.Contains("data-ai-form-version=", StringComparison.Ordinal));
        Assert.Equal(editing && !subtree, html.Contains("<strong>Library</strong>", StringComparison.Ordinal));
        Assert.Equal(editing && !subtree, html.Contains("<strong>Add node</strong>", StringComparison.Ordinal));
        Assert.Equal(editing && !subtree, html.Contains("data-template-management-form-version=", StringComparison.Ordinal));
        Assert.Equal(preview, html.Contains("id=\"dp-preview-form\"", StringComparison.Ordinal));
        Assert.Equal(!published, html.Contains("data-node-wizard", StringComparison.Ordinal));
        if (editing)
        {
            Assert.Contains("action=\"/digital-processes/FormVersionResponse/designer/update-form\"", html);
            Assert.Contains("action=\"/digital-processes/FormVersionResponse/designer/move-form\"", html);
            Assert.Contains("value=\"$.form.section.field\"", html);
        }
        if (preview)
        {
            Assert.Contains("hx-post=\"/digital-processes/form-preview/values\"", html);
            Assert.Contains("name=\"previewInstanceId\"", html);
            Assert.Contains($"name=\"formVersionId\" value=\"{model.designer.form.id}\"", html);
        }
        if (published && !preview)
            Assert.Contains("Published form versions are immutable.", html);
        foreach (var script in new[] { "form-ai-designer", "node-actions", "form-preview" })
            Assert.Contains($"/_content/DigitalProcesses.UI/digital-processes-{script}.js?v=", html);
        Assert.DoesNotContain("function initAiDesigner", html);
        Assert.DoesNotContain("window.digitalProcessesRenumber = ", html);
    }

    [Fact]
    public void EditorViewPreservesQualifiedAncestorPathsAndShowsUnreadablePathsExplicitly()
    {
        var model = Model(false, false, true);
        var node = Assert.Single(model.designer.nodes);
        var view = new FormNodeEditorView(model);
        Assert.Equal("$.form.section.field", view.NodePath(node));
        Assert.Equal([node.id], view.OrderedNodes.Select(item => item.id));
        Assert.Equal([node.id], view.Roots.Select(item => item.id));
        var hiddenAncestor = new FormNodeEditorView(model with { ancestor_nodes = [] });
        Assert.Contains("nelze číst", hiddenAncestor.NodePath(node));
    }

    [Fact]
    public void EditorViewKeepsTreeOrderAndOrphansVisible()
    {
        var model = Model(false, false, false);
        var nodes = model.designer.nodes.ToArray();
        var orphan = nodes[1] with { id = Guid.NewGuid(), parent_id = Guid.NewGuid(), key = "orphan", position = 0 };
        var view = new FormNodeEditorView(model with
        {
            designer = model.designer with { nodes = [orphan, nodes[1], nodes[0]] }
        });
        Assert.Equal([nodes[0].id, nodes[1].id, orphan.id], view.OrderedNodes.Select(node => node.id));
        Assert.Equal([nodes[0].id], view.Containers.Select(node => node.id));
        Assert.Equal(["section", "input.text"], view.OrderedNodeTypes.Select(type => type.renderer_key));
    }

    private static FormDesignerPageResponse Model(bool published, bool preview, bool subtree)
    {
        var versionId = Guid.NewGuid();
        var rbac = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var form = new FormVersionResponse(versionId, Guid.NewGuid(), 1, published, null, rbac, now, now, actor, actor, "Designer test");
        var sectionId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var section = new FormNodeResponse(sectionId, versionId, null, $"/{sectionId:N}", "section", "Section", null,
            DigitalProcessWellKnown.FormNodeTypes.Section, 0, 1, 1, false, null, rbac, now, now, actor, actor);
        var field = new FormNodeResponse(fieldId, versionId, sectionId, $"/{sectionId:N}/{fieldId:N}", "field", "Field", null,
            DigitalProcessWellKnown.FormNodeTypes.Text, 0, 1, 1, false, null, rbac, now, now, actor, actor);
        var types = new FormNodeTypeResponse[]
        {
            new(DigitalProcessWellKnown.FormNodeTypes.Text, null, "Text", "input.text", false, true, false, null, rbac, now, now, actor, actor),
            new(DigitalProcessWellKnown.FormNodeTypes.Section, null, "Section", "section", true, false, true, null, rbac, now, now, actor, actor)
        };
        return new(new(form, subtree ? [field] : [section, field], types, []), preview,
            subtree ? fieldId : null, subtree ? [section] : null);
    }

    private static async Task<string> Render(FormDesignerPageResponse model)
    {
        using var services = new ServiceCollection().AddLogging().AddHttpContextAccessor()
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .AddSingleton<IApplicationNavigationRegistry, ApplicationNavigationRegistry>()
            .AddSingleton<IEntityRouteRegistry, EntityRouteRegistry>()
            .AddDigitalProcessRenderers().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<FormVersionDesignerPage>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { ["Model"] = model }))).ToHtmlString());
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/digital-processes");
    }
}
