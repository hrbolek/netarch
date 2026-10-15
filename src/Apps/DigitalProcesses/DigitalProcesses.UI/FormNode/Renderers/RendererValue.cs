using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.UI.FormNode.Renderers;

internal static class RendererValue
{
    public static string? AsString(SubmissionNodeResponse? value)
    {
        if (string.IsNullOrWhiteSpace(value?.value_json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(value.value_json);
            return doc.RootElement.ValueKind == JsonValueKind.String
                ? doc.RootElement.GetString()
                : doc.RootElement.ToString();
        }
        catch
        {
            return value.value_json;
        }
    }

    public static bool AsBoolean(SubmissionNodeResponse? value)
    {
        if (string.IsNullOrWhiteSpace(value?.value_json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(value.value_json);
            return doc.RootElement.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(doc.RootElement.GetString(), out var result) && result,
                JsonValueKind.Number => doc.RootElement.TryGetInt64(out var n) && n != 0,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}
