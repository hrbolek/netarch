using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;
public static class FormNodeConfiguration
{
    public static JsonObject Parse(string? json)
    {
        try
        {
            return JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json) as JsonObject
                ?? throw new JsonException("Configuration must be a JSON object.");
        }
        catch (JsonException ex)
        {
            throw new JsonException(
                "Invalid configuration_json. Enter a JSON object; line breaks inside the query string must be escaped as \\n, not pasted as literal newlines. " +
                ex.Message, ex);
        }
    }
}
