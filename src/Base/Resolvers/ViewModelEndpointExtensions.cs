using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace WebAppArchitecture.Base.Endpoints;


// ============================================================
// Response mode
// ============================================================

public enum ViewModelResponseMode
{
    Auto,
    Json,
    Html,
    Fragment
}


// ============================================================
// Default preference
// ============================================================

public enum ViewModelDefaultRepresentation
{
    Html,
    Json
}


// ============================================================
// Options
// ============================================================

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

public static class ViewModelEndpointExtensions
{
    /// <summary>
    /// HTML-first endpoint.
    ///
    /// Default:
    ///   no explicit representation -> HTML
    ///
    /// Explicit request can still return JSON or fragment.
    /// </summary>
    public static RouteHandlerBuilder ApplyViewModel<TView, TModel>(
        this RouteHandlerBuilder builder,
        Action<ViewModelOptions>? configure = null
    )
        where TView : IComponent
    {
        return builder.ApplyRepresentation<TView, TModel>(
            options =>
            {
                options.DefaultRepresentation =
                    ViewModelDefaultRepresentation.Html;

                configure?.Invoke(options);
            }
        );
    }


    /// <summary>
    /// JSON-first endpoint.
    ///
    /// Default:
    ///   no explicit representation -> JSON
    ///
    /// Accept: text/html or X-Response-Type: html
    /// returns Razor HTML.
    /// </summary>
    public static RouteHandlerBuilder ApplyJsonModel<TView, TModel>(
        this RouteHandlerBuilder builder,
        Action<ViewModelOptions>? configure = null
    )
        where TView : IComponent
    {
        return builder.ApplyRepresentation<TView, TModel>(
            options =>
            {
                options.DefaultRepresentation =
                    ViewModelDefaultRepresentation.Json;

                configure?.Invoke(options);
            }
        );
    }


    /// <summary>
    /// Common implementation used by ApplyViewModel and
    /// ApplyJsonModel.
    /// </summary>
    public static RouteHandlerBuilder ApplyRepresentation<TView, TModel>(
        this RouteHandlerBuilder builder,
        Action<ViewModelOptions>? configure = null
    )
        where TView : IComponent
    {
        var options =
            new ViewModelOptions();

        configure?.Invoke(options);

        builder.AddEndpointFilter(
            async (
                EndpointFilterInvocationContext context,
                EndpointFilterDelegate next
            ) =>
            {
                // Execute original resolver.
                var result = await next(context);

                if (result is IModelResult modelResult)
                {
                    result = modelResult.Model;
                }

                // Resolver explicitly returned an HTTP result.
                if (result is IResult)
                {
                    return result;
                }                    

                // Null result.
                if (result is null)
                {
                    return options.NullAsNotFound
                        ? Results.NotFound()
                        : Results.NoContent();
                }

                // Type safety.
                if (result is not TModel model)
                {
                    throw new InvalidOperationException(
                        $"Endpoint returned '{result.GetType().FullName}', " +
                        $"but ApplyRepresentation<{typeof(TView).Name}, " +
                        $"{typeof(TModel).Name}> expects " +
                        $"'{typeof(TModel).FullName}'."
                    );
                }

                var request =
                    context.HttpContext.Request;

                var mode =
                    DetermineResponseMode(
                        request,
                        options
                    );

                // JSON
                if (mode == ViewModelResponseMode.Json)
                {
                    return Results.Json(model);
                }

                // HTML / fragment
                var isFragment =
                    mode == ViewModelResponseMode.Fragment;

                var parameters =
                    new Dictionary<string, object?>
                    {
                        [options.ModelParameterName] = model,
                        [options.FragmentParameterName] = isFragment
                    };

                return new RazorComponentResult<TView>(
                    parameters
                );
            }
        );

        return builder;
    }


    /// <summary>
    /// Allways JSON
    /// </summary>
    public static RouteHandlerBuilder ApplyJsonModel<TModel>(
        this RouteHandlerBuilder builder
    )
    {
        builder.AddEndpointFilter(
            async (
                EndpointFilterInvocationContext context,
                EndpointFilterDelegate next
            ) =>
            {
                var result =
                    await next(context);

                // ApiResults.Ok(...) / ApiResults.Model(...)
                // carry a model inside IResult. Unwrap them first.
                if (result is IModelResult modelResult)
                {
                    result = modelResult.Model;
                }

                // Other explicit HTTP responses from resolver
                // are already final (BadRequest, NotFound, File, ...).
                if (result is IResult)
                {
                    return result;
                }

                if (result is null)
                {
                    return Results.NotFound();
                }

                if (result is not TModel model)
                {
                    throw new InvalidOperationException(
                        $"Endpoint returned '{result.GetType().FullName}', " +
                        $"but ApplyJsonModel<{typeof(TModel).Name}> expects " +
                        $"'{typeof(TModel).FullName}'."
                    );
                }

                return Results.Json(model);
            }
        );

        return builder;
    }

    // =========================================================
    // Response negotiation
    // =========================================================

    private static ViewModelResponseMode DetermineResponseMode(
        HttpRequest request,
        ViewModelOptions options
    )
    {
        // Forced mode.
        if (options.ResponseMode != ViewModelResponseMode.Auto)
        {
            return options.ResponseMode;
        }

        // Explicit X-Response-Type header has highest priority.
        if (
            HeaderMatches(
                request,
                options.ResponseHeader,
                options.JsonHeaderValue
            )
        )
        {
            return ViewModelResponseMode.Json;
        }

        if (
            HeaderMatches(
                request,
                options.ResponseHeader,
                options.FragmentHeaderValue
            )
        )
        {
            return ViewModelResponseMode.Fragment;
        }

        if (
            HeaderMatches(
                request,
                options.ResponseHeader,
                options.HtmlHeaderValue
            )
        )
        {
            return ViewModelResponseMode.Html;
        }

        // HTMX fragment request.
        if (
            HeaderMatches(
                request,
                options.FragmentHeader,
                options.FragmentHeaderExpectedValue
            )
        )
        {
            return ViewModelResponseMode.Fragment;
        }

        // Standard Accept negotiation.
        if (options.UseAcceptHeader)
        {
            var acceptMode =
                GetModeFromAcceptHeader(request);

            if (acceptMode is not null)
            {
                return acceptMode.Value;
            }
        }

        // No explicit request: use configured default.
        return options.DefaultRepresentation switch
        {
            ViewModelDefaultRepresentation.Json
                => ViewModelResponseMode.Json,

            _
                => ViewModelResponseMode.Html
        };
    }


    private static ViewModelResponseMode? GetModeFromAcceptHeader(
        HttpRequest request
    )
    {
        var acceptHeaders =
            request
                .GetTypedHeaders()
                .Accept;

        if (
            acceptHeaders is null ||
            acceptHeaders.Count == 0
        )
        {
            return null;
        }

        foreach (var accept in acceptHeaders)
        {
            var mediaType =
                accept.MediaType.Value;

            if (
                string.Equals(
                    mediaType,
                    "application/json",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                (
                    mediaType is not null
                    &&
                    mediaType.EndsWith(
                        "+json",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                return ViewModelResponseMode.Json;
            }

            if (
                string.Equals(
                    mediaType,
                    "text/html",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return ViewModelResponseMode.Html;
            }
        }

        return null;
    }


    private static bool HeaderMatches(
        HttpRequest request,
        string headerName,
        string expectedValue
    )
    {
        if (
            string.IsNullOrWhiteSpace(headerName)
            ||
            string.IsNullOrWhiteSpace(expectedValue)
        )
        {
            return false;
        }

        if (
            !request.Headers.TryGetValue(
                headerName,
                out var values
            )
        )
        {
            return false;
        }

        return values.Any(
            value =>
                string.Equals(
                    value,
                    expectedValue,
                    StringComparison.OrdinalIgnoreCase
                )
        );
    }
}


// ============================================================
// Model result
//
// Carries a successful model through a resolver whose declared
// return type is IResult. Apply* methods can unwrap it and choose
// the final representation (JSON / HTML / fragment).
//
// Without Apply*, ModelResult executes as HTTP 200 JSON.
// ============================================================

public interface IModelResult
{
    object? Model { get; }
}


public sealed class ModelResult<TModel>
    : IResult, IModelResult
{
    public TModel Value { get; }

    object? IModelResult.Model => Value;


    public ModelResult(TModel value)
    {
        Value = value;
    }


    public Task ExecuteAsync(
        HttpContext httpContext
    )
    {
        return Results
            .Ok(Value)
            .ExecuteAsync(httpContext);
    }
}

public static class ApiResults
{
    public static ModelResult<TModel> Model<TModel>(
        TModel model)
    {
        return new ModelResult<TModel>(model);
    }

    public static ModelResult<TModel> Ok<TModel>(
        TModel model)
    {
        return new ModelResult<TModel>(model);
    }

}