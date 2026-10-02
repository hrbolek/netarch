using System.Collections.Concurrent;
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

app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/", () => Results.Ok(new
{
    message = "App běží",

    auth =
        "RBAC actor from X-Actor-UserId header",

    storage =
        "SQLite (User + RBAC)",

    bootstrap_admin_user_id =
        RbacWellKnown.BootstrapAdminUserId,

}));

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
