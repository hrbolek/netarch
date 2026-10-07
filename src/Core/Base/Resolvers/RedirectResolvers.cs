using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.Base.Resolvers;

/// <summary>
/// Conventional convenience redirects for entity endpoint groups.
/// The explicit operation routes remain canonical:
/// /{app}/{type}/list and /{app}/{type}/view/{id}.
/// Query-string state is preserved so vector filters remain permalinkable.
/// </summary>
public static class RedirectResolvers
{
    public static IResult List(HttpContext httpContext)
    {
        var currentPath = httpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        var query = httpContext.Request.QueryString.Value ?? string.Empty;
        return Results.Redirect($"{currentPath}/list{query}", permanent: false, preserveMethod: true);
    }

    public static IResult View(Guid id, HttpContext httpContext)
    {
        var currentPath = httpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        var lastSlash = currentPath.LastIndexOf('/');
        var basePath = lastSlash > 0 ? currentPath[..lastSlash] : currentPath;
        var query = httpContext.Request.QueryString.Value ?? string.Empty;
        return Results.Redirect($"{basePath}/view/{id}{query}", permanent: false, preserveMethod: true);
    }
}
