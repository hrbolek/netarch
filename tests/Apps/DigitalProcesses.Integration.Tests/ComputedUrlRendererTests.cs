using WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Text.Json.Nodes;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class ComputedUrlRendererTests
{
    private readonly FormUrlTemplateRenderer _renderer = new(new FormExpressionEvaluator());

    [Fact]
    public void ConstantHttpsLink_DoesNotRequireFormValues()
    {
        var result = _renderer.Render("https://example.org/path?x=1&y=2", new JsonObject());
        Assert.True(result.Success, result.Error);
        Assert.Equal("https://example.org/path?x=1&y=2", result.Url);
    }

    [Fact]
    public void ComputedLink_ResolvesValueAndEncodesSegment()
    {
        var context = new JsonObject { ["form"] = new JsonObject { ["section"] = new JsonObject { ["user_id"] = "a b" } } };
        var result = _renderer.Render("https://example.org/users/{$.form.section.user_id}", context);
        Assert.True(result.Success, result.Error);
        Assert.Equal("https://example.org/users/a%20b", result.Url);
    }

    [Fact]
    public void DisallowsJavascriptLinks()
    {
        var result = _renderer.Render("javascript:alert(1)", new JsonObject());
        Assert.False(result.Success);
    }
}
