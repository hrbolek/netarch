using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
public static class FormSectionLayout
{
    public static string Read(string? configurationJson)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(configurationJson) ? "{}" : configurationJson);
        if (!document.RootElement.TryGetProperty("visualization", out var value))
            return "stacked";
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not ("stacked" or "table" or "tabs"))
            throw new ArgumentException("Section visualization must be 'stacked', 'table' or 'tabs'.");
        return value.GetString()!;
    }

    public static bool IsTable(FormNodeResponse node) => node.parent_id is not null && Read(node.configuration_json) == "table";
}
