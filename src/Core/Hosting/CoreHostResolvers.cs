using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Routing;

namespace WebAppArchitecture.Hosting;



public static class CoreHostResolvers
{
    public static ApplicationDefinition[] Home(IApplicationNavigationRegistry navigation)
        => navigation.GetApplications().ToArray();

    public static ApplicationDefinition Architecture(IApplicationNavigationRegistry navigation)
        => navigation.Get("architecture")
            ?? throw new InvalidOperationException("Architecture navigation is not registered.");

    public static IResult Info(HttpContext httpContext, ICurrentActorProvider actorProvider)
        => Results.Ok(new
        {
            message = "App běží",
            auth = "Easy Entra proxy headers, claims, or X-Actor-UserId fallback",
            is_authenticated = httpContext.User.Identity?.IsAuthenticated ?? false,
            actor_user_id = actorProvider.TryGetActorUserId(httpContext),
            principal_name = httpContext.User.FindFirst("preferred_username")?.Value
                ?? httpContext.User.Identity?.Name
                ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            storage = "PostgreSQL / Entity Framework Core",
            bootstrap_admin_user_id = RbacWellKnown.BootstrapAdminUserId,
        });

    public static IResult Debug(HttpContext httpContext, ICurrentActorProvider actorProvider)
    {
        var decodedPrincipal = DecodeEasyAuthPrincipal(httpContext.Request.Headers["x-ms-client-principal"]);
        var payload = new
        {
            timestamp_utc = DateTimeOffset.UtcNow,
            path = httpContext.Request.Path.ToString(),
            method = httpContext.Request.Method,
            is_authenticated = httpContext.User.Identity?.IsAuthenticated ?? false,
            actor_user_id = actorProvider.TryGetActorUserId(httpContext),
            principal_name = httpContext.User.FindFirst("preferred_username")?.Value
                ?? httpContext.User.Identity?.Name
                ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            headers = httpContext.Request.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString(), StringComparer.OrdinalIgnoreCase),
            claims = httpContext.User.Claims.Select(claim => new { claim.Type, claim.Value }).ToArray(),
            x_ms_client_principal_decoded = decodedPrincipal,
            diagnostics = new
            {
                has_x_ms_client_principal = httpContext.Request.Headers.ContainsKey("x-ms-client-principal"),
                has_x_ms_client_principal_id = httpContext.Request.Headers.ContainsKey("x-ms-client-principal-id"),
                has_x_ms_client_principal_name = httpContext.Request.Headers.ContainsKey("x-ms-client-principal-name"),
                has_x_fake_easy_entra_id = httpContext.Request.Headers.ContainsKey("x-fake-easy-entra-id")
            }
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        var html = """
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Architecture Debug</title>
<style>body { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, Liberation Mono, monospace; margin: 0; padding: 1rem; background: #0b1220; color: #dbeafe; } h1 { margin-top: 0; font-size: 1.2rem; } .card { background: #0f172a; border: 1px solid #334155; border-radius: 8px; padding: 1rem; } pre { margin: 0; white-space: pre-wrap; word-break: break-word; } a { color: #93c5fd; }</style></head>
<body><h1>/debug</h1><p>Use this page to verify incoming identity headers from fake Easy Entra ID.</p><p><a href="/">Back to /</a></p><div class="card"><pre>__DEBUG_JSON__</pre></div></body></html>
""".Replace("__DEBUG_JSON__", WebUtility.HtmlEncode(json), StringComparison.Ordinal);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static object? DecodeEasyAuthPrincipal(string? encodedPrincipal)
    {
        if (string.IsNullOrWhiteSpace(encodedPrincipal)) return null;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Deserialize<object>(document.RootElement.GetRawText());
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }
}
