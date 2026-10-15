using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>Pure read-only $-rooted JSON property/index resolution; no code execution or remote requests.</summary>
public sealed class FormExpressionEvaluator : IFormExpressionEvaluator
{
    private static readonly Regex Token = new(@"\G(?:\.([A-Za-z_][A-Za-z0-9_]*)|\[(\d+)\])", RegexOptions.Compiled);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public JsonNode? Evaluate(JsonObject context, string expression)
    {
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > 512 || expression[0] != '$')
            throw new FormatException("Expression must start with '$'.");
        JsonNode? current = context;
        var offset = 1;
        while (offset < expression.Length)
        {
            var match = Token.Match(expression, offset);
            if (!match.Success) throw new FormatException($"Invalid JSON path: {expression}");
            if (match.Groups[1].Success)
                current = current is JsonObject obj ? obj[match.Groups[1].Value] : null;
            else
            {
                if (!int.TryParse(match.Groups[2].Value, out var index)) return null;
                current = current is JsonArray array && index < array.Count ? array[index] : null;
            }
            offset += match.Length;
        }
        return current?.DeepClone();
    }
}
