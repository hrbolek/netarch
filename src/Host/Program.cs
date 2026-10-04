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
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Security.Resolvers;
using WebAppArchitecture.Security.Services;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;
using Microsoft.EntityFrameworkCore;


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
var configuredConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=./data/application.db";

// SQLite is used concurrently by multiple request-scoped DbContexts.
// It supports many readers but only one writer at a time. Default Timeout
// makes competing writers wait briefly instead of failing immediately with SQLITE_BUSY.
var connectionString = configuredConnectionString.Contains("Default Timeout=", StringComparison.OrdinalIgnoreCase)
    ? configuredConnectionString
    : configuredConnectionString.TrimEnd(';') + ";Default Timeout=5;Foreign Keys=True";

Directory.CreateDirectory("./data");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDBLayer, DBLayer>();
builder.Services.AddScoped<ISecurityProjectionService, SecurityProjectionService>();
builder.Services.AddScoped<ISecurityLayer, SecurityLayer>();

builder.Services.AddScoped<IUserStore, EfUserStore>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGroupStore, EfGroupStore>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IGroupTypeStore, EfGroupTypeStore>();
builder.Services.AddScoped<IGroupTypeService, GroupTypeService>();
builder.Services.AddScoped<IRoleStore, EfRoleStore>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IMembershipStore, EfMembershipStore>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IRoleTypeStore, EfRoleTypeStore>();
builder.Services.AddScoped<IRoleTypeService, RoleTypeService>();
builder.Services.AddScoped<IBaseStore<UserModel>>(sp => sp.GetRequiredService<IUserStore>());
builder.Services.AddScoped<IBaseStore<GroupModel>>(sp => sp.GetRequiredService<IGroupStore>());
builder.Services.AddScoped<IBaseStore<GroupTypeModel>>(sp => sp.GetRequiredService<IGroupTypeStore>());
builder.Services.AddScoped<IBaseStore<RoleModel>>(sp => sp.GetRequiredService<IRoleStore>());
builder.Services.AddScoped<IBaseStore<MembershipModel>>(sp => sp.GetRequiredService<IMembershipStore>());
builder.Services.AddScoped<IBaseStore<RoleTypeModel>>(sp => sp.GetRequiredService<IRoleTypeStore>());


builder.Services.AddSingleton<ICurrentActorProvider, HeaderOrClaimActorProvider>();
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<ISecurityService>(sp => sp.GetRequiredService<SecurityService>());
builder.Services.AddScoped<ISecurityPermissionService, SecurityPermissionService>();
builder.Services.AddScoped<IRbacService>(sp => sp.GetRequiredService<SecurityService>());
builder.Services.AddScoped<IRbacAuthorizationService>(sp => sp.GetRequiredService<SecurityService>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var systemDataSource = builder.Configuration["SystemData:Source"]
        ?? Environment.GetEnvironmentVariable("SYSTEMDATA_SOURCE")
        ?? SystemDataImporter.DefaultSource;
    var importSystemData = builder.Configuration.GetValue("SystemData:Import", true);
    await DatabaseInitializer.InitializeAsync(db, systemDataSource, importSystemData);
    var securityProjection = scope.ServiceProvider.GetRequiredService<ISecurityProjectionService>();
    await securityProjection.RebuildAllAsync();
}

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
        "SQLite / EF Core (single application database)",

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
app.MapSecurityEndpoints("/architecture");

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
