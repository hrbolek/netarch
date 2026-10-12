using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace WebAppArchitecture.Base.Endpoints;


// ============================================================
// Response mode
// ============================================================

/// <summary>
/// Typ View Options ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed class ViewModelOptions
{
    /// <summary>
    /// Default representation used when the request does not
    /// explicitly ask for JSON, HTML or fragment.
    /// </summary>
    public ViewModelDefaultRepresentation DefaultRepresentation { get; set; }
        = ViewModelDefaultRepresentation.Html;

    /// <summary>
    /// Optional forced response mode.
    ///
    /// Auto = use content negotiation and DefaultRepresentation.
    /// </summary>
    public ViewModelResponseMode ResponseMode { get; set; }
        = ViewModelResponseMode.Auto;

    /// <summary>
    /// Enables Accept header negotiation.
    /// </summary>
    public bool UseAcceptHeader { get; set; }
        = true;

    /// <summary>
    /// Explicit representation header.
    ///
    /// Default:
    /// X-Response-Type: json
    /// X-Response-Type: html
    /// X-Response-Type: fragment
    /// </summary>
    public string ResponseHeader { get; set; }
        = "X-Response-Type";

    public string JsonHeaderValue { get; set; }
        = "json";

    public string HtmlHeaderValue { get; set; }
        = "html";

    public string FragmentHeaderValue { get; set; }
        = "fragment";

    /// <summary>
    /// HTMX-compatible fragment detection.
    ///
    /// HX-Request: true
    /// </summary>
    public string FragmentHeader { get; set; }
        = "HX-Request";

    public string FragmentHeaderExpectedValue { get; set; }
        = "true";

    /// <summary>
    /// Razor component parameter receiving TModel.
    ///
    /// [Parameter]
    /// public TModel Model { get; set; }
    /// </summary>
    public string ModelParameterName { get; set; }
        = "Model";

    /// <summary>
    /// Razor component parameter receiving fragment flag.
    ///
    /// [Parameter]
    /// public bool IsFragment { get; set; }
    /// </summary>
    public string FragmentParameterName { get; set; }
        = "IsFragment";

    /// <summary>
    /// If resolver returns null, return HTTP 404.
    /// Otherwise HTTP 204.
    /// </summary>
    public bool NullAsNotFound { get; set; }
        = true;
}


// ============================================================
// Endpoint extensions
// ============================================================
