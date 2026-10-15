using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using System.Text.Json;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormNodeConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyConfiguration_IsAnEmptyObject(string? json)
        => Assert.Empty(FormNodeConfiguration.Parse(json));

    [Theory]
    [InlineData("{\"query\":\"query {\r\n choices { id label } }\"}")]
    [InlineData("{\"query\":\"query {\n choices { id label } }\"}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"query\"")]
    public void InvalidConfiguration_IsRejectedWithAnExplanation(string json)
    {
        var error = Assert.Throws<JsonException>(() => FormNodeConfiguration.Parse(json));
        Assert.Contains("configuration_json", error.Message);
        Assert.Contains("\\n", error.Message);
    }

    [Fact]
    public void EscapedMultilineQuery_IsPreserved()
    {
        var config = FormNodeConfiguration.Parse("{\r\n \"query\": \"query {\\r\\n choices { id label } }\" \r\n}");
        Assert.Equal("query {\r\n choices { id label } }", config["query"]!.GetValue<string>());
    }
}
