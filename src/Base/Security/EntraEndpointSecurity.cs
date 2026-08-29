using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace WebAppArchitecture.Base.Security;

public static class EntraEndpointSecurity
{
    public const string AuthenticationScheme =
        JwtBearerDefaults.AuthenticationScheme;

    public sealed record SecurityMetadata(
        bool RequiresEntra,
        IReadOnlyCollection<string> Roles
    );

    // =========================================================
    // Service registration
    // =========================================================

    public static IServiceCollection AddEntraEndpointSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "AzureAd"
    )
    {
        services
            .AddAuthentication(AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(
                configuration.GetSection(configurationSection)
            );

        services.AddAuthorization();

        return services;
    }

    // =========================================================
    // Protected endpoint
    //
    // WithEntra("Signer")
    // WithEntra("Signer", "Administrator")
    //
    // Multiple roles mean OR:
    // Signer OR Administrator
    // =========================================================

    public static RouteHandlerBuilder WithEntra(
        this RouteHandlerBuilder builder,
        params string[] roles
    )
    {
        if (roles is null || roles.Length == 0)
        {
            throw new ArgumentException(
                "WithEntra requires at least one role.",
                nameof(roles)
            );
        }

        var normalizedRoles = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedRoles.Length == 0)
        {
            throw new ArgumentException(
                "WithEntra requires at least one non-empty role.",
                nameof(roles)
            );
        }

        builder.WithMetadata(
            new SecurityMetadata(
                RequiresEntra: true,
                Roles: normalizedRoles
            )
        );

        builder.RequireAuthorization(policy =>
        {
            policy.AddAuthenticationSchemes(
                AuthenticationScheme
            );

            policy.RequireAuthenticatedUser();

            // Multiple roles here have OR semantics.
            policy.RequireRole(normalizedRoles);
        });

        return builder;
    }

    // =========================================================
    // Explicitly public endpoint
    // =========================================================

    public static RouteHandlerBuilder WithoutEntra(
        this RouteHandlerBuilder builder
    )
    {
        builder.WithMetadata(
            new SecurityMetadata(
                RequiresEntra: false,
                Roles: Array.Empty<string>()
            )
        );

        builder.AllowAnonymous();

        return builder;
    }

    // =========================================================
    // Runtime validation
    //
    // Every RouteEndpoint must contain exactly one
    // SecurityMetadata declaration.
    // =========================================================

    public static void ValidateEntraEndpointSecurity(
        this WebApplication app
    )
    {
        var dataSources =
            app.Services.GetServices<EndpointDataSource>();

        var errors = new List<string>();

        foreach (var dataSource in dataSources)
        {
            foreach (var endpoint in dataSource.Endpoints)
            {
                if (endpoint is not RouteEndpoint routeEndpoint)
                    continue;

                var declarations =
                    endpoint.Metadata
                        .GetOrderedMetadata<SecurityMetadata>();

                var route =
                    routeEndpoint.RoutePattern.RawText
                    ?? endpoint.DisplayName
                    ?? "<unknown>";

                if (declarations.Count == 0)
                {
                    errors.Add(
                        $"Endpoint '{route}' is missing security declaration. " +
                        "Use WithEntra(...) or WithoutEntra()."
                    );

                    continue;
                }

                if (declarations.Count > 1)
                {
                    errors.Add(
                        $"Endpoint '{route}' contains multiple security " +
                        "declarations. Use exactly one WithEntra(...) " +
                        "or WithoutEntra()."
                    );

                    continue;
                }

                var declaration = declarations[0];

                if (
                    declaration.RequiresEntra &&
                    declaration.Roles.Count == 0
                )
                {
                    errors.Add(
                        $"Endpoint '{route}' requires Entra authentication " +
                        "but defines no roles."
                    );
                }

                if (
                    !declaration.RequiresEntra &&
                    declaration.Roles.Count > 0
                )
                {
                    errors.Add(
                        $"Endpoint '{route}' is marked WithoutEntra() " +
                        "but contains roles."
                    );
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Invalid endpoint security configuration:"
                + Environment.NewLine
                + Environment.NewLine
                + string.Join(
                    Environment.NewLine,
                    errors.Select(error => " - " + error)
                )
            );
        }
    }
}