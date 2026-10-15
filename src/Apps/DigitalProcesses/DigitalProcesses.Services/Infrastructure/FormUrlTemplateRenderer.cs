using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>Replaces {$....} expressions with escaped URI component values. A missing
/// reference fails closed; only absolute HTTP(S) URLs are supported.</summary>
public sealed class FormUrlTemplateRenderer(IFormExpressionEvaluator evaluator) : IFormUrlTemplateRenderer
{
    private static readonly Regex Placeholder = new(@"\{(\$[^{}]+)\}", RegexOptions.Compiled);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public FormUrlResult Render(string template, JsonObject context)
    {
        if (string.IsNullOrWhiteSpace(template)) return new(null, "URL template is empty.");
        if (template.Length > 4096) return new(null, "URL template exceeds 4096 characters.");
        if (Placeholder.Matches(template).Count > 32) return new(null, "Too many URL placeholders.");
        string? error = null;
        string replaced;
        try
        {
            replaced = Placeholder.Replace(template, match =>
            {
                var value = evaluator.Evaluate(context, match.Groups[1].Value);
                if (value is not JsonValue scalar || scalar.ToString().Length == 0)
                {
                    error = $"Missing value for {match.Groups[1].Value}.";
                    return string.Empty;
                }
                return Uri.EscapeDataString(scalar.ToString());
            });
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException or OverflowException or System.Text.Json.JsonException)
        { return new(null, "Invalid expression or value in URL template."); }
        if (error is not null) return new(null, error);
        if (replaced.Contains("{$", StringComparison.Ordinal)) return new(null, "Invalid URL placeholder.");
        if (replaced.Length > 8192) return new(null, "Computed URL is too long.");
        if (!Uri.TryCreate(replaced, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(url.UserInfo))
            return new(null, "Only absolute HTTP(S) URLs without credentials are allowed.");
        return new(url.AbsoluteUri, null);
    }
}
