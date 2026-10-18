using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

public sealed class HeaderOrClaimActorProvider : ICurrentActorProvider
{
    public Guid? TryGetActorUserId(HttpContext httpContext)
    {
        if (TryParseClaim(httpContext.User, "oid", out var oid))
            return oid;

        if (TryParseClaim(httpContext.User, "sub", out var sub))
            return sub;

        if (TryParseClaim(httpContext.User, ClaimTypes.NameIdentifier, out var nid))
            return nid;

        if (TryParseValue(httpContext.Request.Headers["x-ms-client-principal-id"], out var easyAuthHeaderActor))
            return easyAuthHeaderActor;

        if (TryParseEasyAuthPrincipalActor(httpContext.Request.Headers["x-ms-client-principal"], out var easyAuthPrincipalActor))
            return easyAuthPrincipalActor;

        if (TryParseValue(httpContext.Request.Headers["X-Actor-UserId"], out var headerActor))
            return headerActor;

        return null;
    }

    private static bool TryParseClaim(ClaimsPrincipal principal, string claimType, out Guid userId)
    {
        userId = Guid.Empty;
        var value = principal.FindFirstValue(claimType);
        return TryParseValue(value, out userId);
    }

    private static bool TryParseValue(string? value, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Guid.TryParse(value, out userId);
    }

    private static bool TryParseEasyAuthPrincipalActor(string? encodedPrincipal, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(encodedPrincipal))
            return false;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
            var payload = JsonSerializer.Deserialize<EasyAuthPrincipalPayload>(json, EasyAuthPrincipalJsonOptions);
            if (payload?.claims is null)
                return false;

            foreach (var claim in payload.claims)
            {
                if (claim is null || !string.Equals(claim.typ, "oid", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryParseValue(claim.val, out userId))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static readonly JsonSerializerOptions EasyAuthPrincipalJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
