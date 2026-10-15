using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.DigitalProcesses.UI.FormVersion.Components;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class EntityCardsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FormDefinitionUsesOneAttributeCardForListsDetailsAndCustomContent(bool nullDescription)
    {
        var now = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        var model = new WebAppArchitecture.DigitalProcesses.Models.FormDefinition.FormDefinitionResponse(
            Guid.NewGuid(), "Unified definition", nullDescription ? null : "Definition description",
            Guid.NewGuid(), now, now.AddHours(1), Guid.NewGuid(), Guid.NewGuid(), "Definition label");
        var medium = Component("FormDefinition", "MediumCard");
        var large = Component("FormDefinition", "LargeCard");
        Assert.Null(medium.Assembly.GetType(
            "WebAppArchitecture.DigitalProcesses.UI.FormDefinition.Components.FormDefinitionModelCard"));
        var standalone = await Render(medium, new() { ["Model"] = model });
        foreach (var text in new[]
        {
            model.name, model.visual_label, model.id.ToString(), model.rbacobject_id.ToString(),
            model.created_by.ToString(), model.updated_by.ToString(),
            model.created_at.LocalDateTime.ToString("g", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ")),
            model.updated_at.LocalDateTime.ToString("g", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ"))
        })
            Assert.Contains(text, WebUtility.HtmlDecode(standalone));
        if (!nullDescription)
            Assert.Contains(model.description!, standalone);
        foreach (var custom in new[] { false, true })
        {
            var parameters = new Dictionary<string, object?> { ["Model"] = model };
            if (custom)
                parameters["ChildContent"] = (RenderFragment<WebAppArchitecture.DigitalProcesses.Models.FormDefinition.FormDefinitionResponse>)
                    (_ => builder => builder.AddContent(0, "Custom definition content"));
            var html = await Render(large, parameters);
            Assert.Equal(1, Regex.Matches(html, "<b>name</b>").Count);
            Assert.Equal(1, Regex.Matches(html, "Raw JSON \\(diagnostics\\)").Count);
            Assert.Contains("Valid Permissions", html);
            Assert.Contains($"/digital-processes/FormDefinitionResponse/view/{model.id}", html);
            Assert.Contains($"/digital-processes/FormDefinitionResponse/actions/{model.id}", html);
            Assert.Equal(custom, html.Contains("Custom definition content", StringComparison.Ordinal));
        }
    }

    public static IEnumerable<object[]> Entities() =>
        typeof(FormVersionLargeCard).Assembly.GetTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type) &&
                type.Name.EndsWith("LargeCard", StringComparison.Ordinal) &&
                type.Namespace?.StartsWith("WebAppArchitecture.DigitalProcesses.UI.", StringComparison.Ordinal) == true)
            .Select(type => new object[] { type.Name[..^"LargeCard".Length] });

    [Theory]
    [MemberData(nameof(Entities))]
    public async Task TypedCardsPreserveFieldsDiagnosticsAndNavigation(string entity)
    {
        var dto = typeof(FormVersionResponse).Assembly.GetType(
            $"WebAppArchitecture.DigitalProcesses.Models.{entity}.{entity}Response", throwOnError: true)!;
        var constructor = dto.GetConstructors().Single(ctor => ctor.IsPublic);
        foreach (var nullOptionals in new[] { true, false })
        {
            var model = constructor.Invoke(constructor.GetParameters()
                .Select(parameter => SampleValue(parameter.ParameterType, parameter.Name!, nullOptionals)).ToArray());
            foreach (var property in dto.GetProperties().Where(property => property.SetMethod is not null))
                property.SetValue(model, SampleValue(property.PropertyType, property.Name, nullOptionals));
            var id = (Guid)dto.GetProperty("id")!.GetValue(model)!;
            var modelCard = Component(entity, entity == "FormDefinition" ? "MediumCard" : "ModelCard");
            var html = await Render(modelCard, new() { ["Model"] = model });
            var decoded = WebUtility.HtmlDecode(html);
            var fields = dto.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name != "visual_label" &&
                    !(entity == "FormVersion" && property.Name == "templates") &&
                    !(entity == "Request" && property.Name == "workspace")).ToArray();
            var labels = Regex.Matches(decoded, @"<dt[^>]*>(.*?)</dt>").Select(match => match.Groups[1].Value).ToArray();
            var values = Regex.Matches(decoded, @"<dd[^>]*>(.*?)</dd>").Select(match => match.Groups[1].Value).ToArray();
            if (entity == "FormDefinition")
            {
                Assert.DoesNotContain("<dt", html);
                Assert.Equal(fields.Length + 1, Regex.Matches(html, "class=\"row Attribute_Row\"").Count);
                foreach (var property in fields)
                    Assert.Contains($"<b>{property.Name.Replace('_', ' ')}</b>", decoded);
            }
            else
            {
                Assert.Equal(fields.Select(property => property.Name.Replace('_', ' ')), labels);
                Assert.Equal(fields.Select(property => property.GetValue(model)?.ToString() ?? "\u2014"), values);
            }
            Assert.Contains("Raw JSON (diagnostics)", html);
            Assert.Contains($"href=\"/digital-processes/{entity}Response/view/{id}\"", html);
            if (entity == "FormVersion")
            {
                Assert.Contains($"/digital-processes/FormVersionResponse/designer/{id}", html);
                Assert.Contains($"/digital-processes/FormVersionResponse/test/{id}", html);
            }
            if (entity == "WorkflowVersion")
                Assert.Contains($"/digital-processes/WorkflowVersionResponse/designer/{id}", html);
            if (entity == "ProcessDefinitionVersion")
            {
                Assert.Contains($"action=\"/digital-processes/ProcessDefinitionVersionResponse/test/{id}\"", html);
                Assert.Contains("return confirm(", html);
            }
            foreach (var suffix in new[] { "MediumCard", "LargeCard" })
            {
                var card = Component(entity, suffix);
                Assert.False(card.IsGenericType);
                Assert.Equal(dto, card.GetProperty("Model")!.PropertyType);
                html = await Render(card, new() { ["Model"] = model });
                Assert.Contains($"/digital-processes/{entity}Response/actions/{id}", html);
                if (suffix == "LargeCard")
                {
                    Assert.Contains("Raw JSON (diagnostics)", html);
                    Assert.Contains("Valid Permissions", html);
                }
            }
        }
    }

    [Theory]
    [InlineData("ProcessDefinition", "form_definition_id", "FormDefinition")]
    [InlineData("ProcessDefinition", "workflow_definition_id", "WorkflowDefinition")]
    [InlineData("FormVersion", "form_definition_id", "FormDefinition")]
    [InlineData("WorkflowVersion", "workflow_definition_id", "WorkflowDefinition")]
    [InlineData("ProcessDefinitionVersion", "process_definition_id", "ProcessDefinition")]
    [InlineData("ProcessDefinitionVersion", "form_version_id", "FormVersion")]
    [InlineData("ProcessDefinitionVersion", "workflow_version_id", "WorkflowVersion")]
    [InlineData("FormNode", "form_version_id", "FormVersion")]
    [InlineData("FormNode", "parent_id", "FormNode")]
    [InlineData("WorkflowState", "workflow_version_id", "WorkflowVersion")]
    [InlineData("WorkflowTransition", "workflow_version_id", "WorkflowVersion")]
    [InlineData("WorkflowTransition", "source_state_id", "WorkflowState")]
    [InlineData("WorkflowTransition", "target_state_id", "WorkflowState")]
    [InlineData("Request", "process_definition_version_id", "ProcessDefinitionVersion")]
    [InlineData("Request", "current_state_id", "WorkflowState")]
    [InlineData("Request", "current_submission_id", "Submission")]
    [InlineData("Submission", "request_id", "Request")]
    [InlineData("Submission", "form_version_id", "FormVersion")]
    public async Task DomainRelationsPreserveTargetsAndOmitEmptyIds(string entity, string propertyName, string target)
    {
        var component = Component(entity, "ForeignKeyLinks");
        var dto = component.GetProperty("Model")!.PropertyType;
        var model = dto.GetConstructors().Single(ctor => ctor.IsPublic).Invoke(
            dto.GetConstructors().Single(ctor => ctor.IsPublic).GetParameters()
                .Select(parameter => SampleValue(parameter.ParameterType, parameter.Name!, true)).ToArray());
        var id = Guid.NewGuid();
        dto.GetProperty(propertyName)!.SetValue(model, id);
        var html = await Render(component, new() { ["Model"] = model });
        Assert.Contains($"/digital-processes/{target}Response/view/{id}", html);
        dto.GetProperty(propertyName)!.SetValue(model, Guid.Empty);
        html = await Render(component, new() { ["Model"] = model });
        Assert.DoesNotContain($"/view/{Guid.Empty}", html);
        Assert.DoesNotContain($"/view/{id}", html);
    }

    [Fact]
    public async Task CustomContentPreservesFormVersionNavigationAndPermissions()
    {
        var id = Guid.NewGuid();
        var rbac = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var model = new FormVersionResponse(id, Guid.NewGuid(), 1, false, null, rbac, now, now, Guid.NewGuid(), Guid.NewGuid());
        RenderFragment<FormVersionResponse> content = version => builder => builder.AddContent(0, $"Custom version {version.version}");
        var html = await Render(typeof(FormVersionLargeCard), new() { ["Model"] = model, ["ChildContent"] = content });
        Assert.Contains("Custom version 1", html);
        Assert.DoesNotContain("Raw JSON (diagnostics)", html);
        Assert.Contains($"/digital-processes/FormVersionResponse/designer/{id}", html);
        Assert.Contains($"/digital-processes/FormVersionResponse/test/{id}", html);
        Assert.Contains($"/digital-processes/FormVersionResponse/templates/{id}", html);
        Assert.Contains($"/architecture/RbacPermissionResponse/permissions/{rbac}", html);
    }

    private static Type Component(string entity, string suffix) =>
        typeof(FormVersionLargeCard).Assembly.GetType(
            $"WebAppArchitecture.DigitalProcesses.UI.{entity}.Components.{entity}{suffix}", throwOnError: true)!;

    private static object? SampleValue(Type type, string name, bool nullOptionals)
    {
        if (type == typeof(WebAppArchitecture.DigitalProcesses.Models.Request.RequestWorkspaceResponse))
            return null;
        if (type == typeof(IReadOnlyList<WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate.RtfTemplateInfo>))
            return nullOptionals ? null : Array.Empty<WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate.RtfTemplateInfo>();
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return nullOptionals ? null : SampleValue(underlying, name, false);
        if (type == typeof(string)) return name is "description" or "note" && nullOptionals ? null : name + " value";
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(int)) return 2;
        if (type == typeof(bool)) return true;
        if (type == typeof(DateTimeOffset)) return new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        throw new NotSupportedException($"No sample value for {type}.");
    }

    private static async Task<string> Render(Type component, Dictionary<string, object?> parameters)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddHttpContextAccessor()
            .AddSingleton<IEntityRouteRegistry>(Routes())
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync(component, ParameterView.FromDictionary(parameters))).ToHtmlString());
    }

    private static IEntityRouteRegistry Routes()
    {
        var registry = new EntityRouteRegistry();
        foreach (var dto in typeof(FormVersionResponse).Assembly.GetTypes().Where(type =>
                     type.Namespace?.StartsWith("WebAppArchitecture.DigitalProcesses.Models.", StringComparison.Ordinal) == true &&
                     typeof(WebAppArchitecture.Base.Data.IEntityDTOModel).IsAssignableFrom(type) &&
                     type.Name.EndsWith("Response", StringComparison.Ordinal)))
        {
            typeof(EntityRouteRegistry).GetMethod(nameof(EntityRouteRegistry.Register))!
                .MakeGenericMethod(dto).Invoke(registry, ["digital-processes", null, null]);
        }
        return registry;
    }
}
