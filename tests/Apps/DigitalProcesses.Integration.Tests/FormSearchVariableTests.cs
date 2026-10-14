using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormSearchVariableTests
{
    [Fact]
    public void Resolve_UsesCurrentFormValuesForConfiguredSearchVariables()
    {
        var request = new FormBackendOperationRequest(
            "query search($pattern: Str, $projectCode: Str) { choices { id label } }",
            Array.Empty<FormBackendParameterBinding>(),
            new JsonObject
            {
                ["pattern"] = "$.form.searchTerm",
                ["projectCode"] = "$.form.basic_information.project_code",
                ["allFormValues"] = "$.form"
            });
        var snapshot = new FormValueSnapshot(
            new Dictionary<string, JsonNode?>
            {
                ["searchTerm"] = JsonValue.Create("alice"),
                ["basic_information.project_code"] = JsonValue.Create("PRJ-123")
            },
            new Dictionary<string, JsonNode?>());

        var variables = FormVariableResolver.Resolve(request, snapshot);

        Assert.Equal("alice", variables["pattern"]!.GetValue<string>());
        Assert.Equal("PRJ-123", variables["projectCode"]!.GetValue<string>());
        Assert.Equal("PRJ-123", variables["allFormValues"]!["basic_information"]!["project_code"]!.GetValue<string>());
    }
}
