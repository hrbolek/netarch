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

public static class EasyEntraHeaderPrincipalExtensions
{
    public static IApplicationBuilder UseEasyEntraHeaderPrincipal(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (httpContext, next) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true &&
                TryBuildPrincipal(httpContext.Request.Headers, out var principal))
            {
                httpContext.User = principal;
            }

            await next();
        });
    }

    private static bool TryBuildPrincipal(IHeaderDictionary headers, out ClaimsPrincipal principal)
    {
        principal = new ClaimsPrincipal(new ClaimsIdentity());
        var claims = new List<Claim>();


        if (TryDecodeEasyAuthPrincipal(headers["x-ms-client-principal"], claims) ||
            TryBuildFallbackClaims(headers, claims))
        {
            principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "EasyEntraProxy", ClaimTypes.Name, ClaimTypes.Role));
            return true;
        }

        return false;
    }

    private static bool TryDecodeEasyAuthPrincipal(string? encodedPrincipal, List<Claim> claims)
    {
        if (string.IsNullOrWhiteSpace(encodedPrincipal))
            return false;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
            var payload = JsonSerializer.Deserialize<EasyAuthPrincipalPayload>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload?.claims is null || payload.claims.Count == 0)
                return false;

            foreach (var claim in payload.claims)
            {
                if (claim is null || string.IsNullOrWhiteSpace(claim.typ) || string.IsNullOrWhiteSpace(claim.val))
                    continue;

                AddClaimMappings(claims, claim.typ, claim.val);
            }

            return claims.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryBuildFallbackClaims(IHeaderDictionary headers, List<Claim> claims)
    {
        var principalId = headers["x-ms-client-principal-id"].ToString();
        var principalName = headers["x-ms-client-principal-name"].ToString();

        if (string.IsNullOrWhiteSpace(principalId) && string.IsNullOrWhiteSpace(principalName))
            return false;

        if (!string.IsNullOrWhiteSpace(principalId))
        {
            AddClaimMappings(claims, "oid", principalId);
            AddClaimMappings(claims, "sub", principalId);
        }

        if (!string.IsNullOrWhiteSpace(principalName))
        {
            AddClaimMappings(claims, "name", principalName);
            AddClaimMappings(claims, "preferred_username", principalName);
            AddClaimMappings(claims, "email", principalName);
        }

        return claims.Count > 0;
    }

    private static void AddClaimMappings(List<Claim> claims, string claimType, string claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimType) || string.IsNullOrWhiteSpace(claimValue))
            return;

        claims.Add(new Claim(claimType, claimValue));

        if (string.Equals(claimType, "roles", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var role in claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                claims.Add(new Claim(ClaimTypes.Role, role));

            return;
        }

        if (string.Equals(claimType, "scp", StringComparison.OrdinalIgnoreCase))
        {
            var scopes = claimValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var scope in scopes)
            {
                claims.Add(new Claim("scp", scope));
                claims.Add(new Claim("scope", scope));
            }

            return;
        }

        if (string.Equals(claimType, "oid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claimType, "sub", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, claimValue));
            return;
        }

        if (string.Equals(claimType, "name", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Name, claimValue));
            return;
        }

        if (string.Equals(claimType, "email", StringComparison.OrdinalIgnoreCase))
            claims.Add(new Claim(ClaimTypes.Email, claimValue));
    }
}
