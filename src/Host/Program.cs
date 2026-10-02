using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using WebAppArchitecture;

// ===========================================================
// TODO 1. Určení jmenných prostorů a názvů tříd z template
// ===========================================================
using WebAppArchitecture.User.Resolvers;
using WebAppArchitecture.User.Services;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.Group.Resolvers;
using WebAppArchitecture.Group.Services;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.Role.Resolvers;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Membership.Resolvers;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.RoleType.Resolvers;
using WebAppArchitecture.RoleType.Services;
using WebAppArchitecture.RoleType.Models;
using WebAppArchitecture.Base.Security;


var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Services
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorComponents();

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<HostOperationFilter>();
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
});


// ============================================================
// TODO 2. Dependency Injection z template
// ============================================================
var userStoreConnectionString =
    builder.Configuration.GetConnectionString("UserStore")
    ?? "Data Source=./data/user-store.db";

var rbacStoreConnectionString =
    builder.Configuration.GetConnectionString("RbacStore")
    ?? "Data Source=./data/rbac-store.db";

builder.Services.AddSqliteRbac(rbacStoreConnectionString);

builder.Services.AddSingleton<IUserStore>(_ => new SqliteUserStore(userStoreConnectionString));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IGroupStore>(_ => new SqliteGroupStore(rbacStoreConnectionString));
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddSingleton<IRoleStore>(_ => new SqliteRoleStore(rbacStoreConnectionString));
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddSingleton<IMembershipStore>(_ => new SqliteMembershipStore(rbacStoreConnectionString));
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddSingleton<IRoleTypeStore>(_ => new SqliteRoleTypeStore(rbacStoreConnectionString));
builder.Services.AddScoped<IRoleTypeService, RoleTypeService>();

var app = builder.Build();

// ============================================================
// Swagger
// ============================================================

app.UseEasyEntraHeaderPrincipal();
app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/", (HttpContext httpContext, ICurrentActorProvider actorProvider) => Results.Ok(new
{
    message = "App běží",

    auth =
        "Easy Entra proxy headers, claims, or X-Actor-UserId fallback",

    is_authenticated =
        httpContext.User.Identity?.IsAuthenticated ?? false,

    actor_user_id =
        actorProvider.TryGetActorUserId(httpContext),

    principal_name =
        httpContext.User.FindFirst("preferred_username")?.Value
        ?? httpContext.User.Identity?.Name
        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,

    storage =
        "SQLite (User + RBAC)",

    bootstrap_admin_user_id =
        RbacWellKnown.BootstrapAdminUserId,

}));

app.MapGet("/debug", (HttpContext httpContext, ICurrentActorProvider actorProvider) =>
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
                headers = httpContext.Request.Headers
                        .ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.ToString(),
                                StringComparer.OrdinalIgnoreCase),
                claims = httpContext.User.Claims
                        .Select(claim => new { claim.Type, claim.Value })
                        .ToArray(),
                x_ms_client_principal_decoded = decodedPrincipal,
                diagnostics = new
                {
                        has_x_ms_client_principal = httpContext.Request.Headers.ContainsKey("x-ms-client-principal"),
                        has_x_ms_client_principal_id = httpContext.Request.Headers.ContainsKey("x-ms-client-principal-id"),
                        has_x_ms_client_principal_name = httpContext.Request.Headers.ContainsKey("x-ms-client-principal-name"),
                        has_x_fake_easy_entra_id = httpContext.Request.Headers.ContainsKey("x-fake-easy-entra-id")
                }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
                WriteIndented = true
        });

        var html = """
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>SignFakeArchitecture Debug</title>
    <style>
        body { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, Liberation Mono, monospace; margin: 0; padding: 1rem; background: #0b1220; color: #dbeafe; }
        h1 { margin-top: 0; font-size: 1.2rem; }
        .card { background: #0f172a; border: 1px solid #334155; border-radius: 8px; padding: 1rem; }
        pre { margin: 0; white-space: pre-wrap; word-break: break-word; }
        a { color: #93c5fd; }
    </style>
</head>
<body>
    <h1>/debug</h1>
    <p>Use this page to verify incoming identity headers from fake Easy Entra ID.</p>
    <p><a href="/">Back to /</a></p>
    <div class="card">
        <pre>__DEBUG_JSON__</pre>
    </div>
</body>
</html>
""".Replace("__DEBUG_JSON__", WebUtility.HtmlEncode(json), StringComparison.Ordinal);

        return Results.Content(html, "text/html; charset=utf-8");
});

// ============================================================
// TODO 3. Mapování endpointů z template
// ============================================================
app.MapUserEndpoints("/architecture");
app.MapGroupEndpoints("/architecture");
app.MapRoleEndpoints("/architecture");
app.MapMembershipEndpoints("/architecture");
app.MapRoleTypeEndpoints("/architecture");
app.MapRbacEndpoints("/architecture/rbac");

// ============================================================
// Start
// ============================================================

app.Run(
    "http://0.0.0.0:8080"
);

static object? DecodeEasyAuthPrincipal(string? encodedPrincipal)
{
    if (string.IsNullOrWhiteSpace(encodedPrincipal))
        return null;

    try
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Deserialize<object>(document.RootElement.GetRawText());
    }
    catch (Exception ex)
    {
        return new
        {
            error = "x-ms-client-principal decode failed",
            exception = ex.Message,
            raw_value = encodedPrincipal
        };
    }
}
