using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Pages;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using WebAppArchitecture;
using WebAppArchitecture.Host;

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
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.Resolvers;
using WebAppArchitecture.DigitalProcesses.UI.Routing;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEntityRouteRegistry>(sp =>
{
    var registry = new EntityRouteRegistry();
    registry.Register<WebAppArchitecture.Group.Models.GroupTypeResponse>(operation: "read");
    registry.Register<FormDefinitionResponse>(app: "digital-processes");
    registry.Register<FormVersionResponse>(app: "digital-processes");
    registry.Register<FormNodeTypeResponse>(app: "digital-processes");
    registry.Register<FormNodeResponse>(app: "digital-processes");
    registry.Register<WorkflowDefinitionResponse>(app: "digital-processes");
    registry.Register<WorkflowVersionResponse>(app: "digital-processes");
    registry.Register<WorkflowStateResponse>(app: "digital-processes");
    registry.Register<WorkflowStateAccessRuleResponse>(app: "digital-processes");
    registry.Register<WorkflowTransitionResponse>(app: "digital-processes");
    registry.Register<ProcessDefinitionResponse>(app: "digital-processes");
    registry.Register<ProcessDefinitionVersionResponse>(app: "digital-processes");
    registry.Register<RequestResponse>(app: "digital-processes");
    registry.Register<SubmissionResponse>(app: "digital-processes");
    registry.Register<SubmissionNodeResponse>(app: "digital-processes");
    registry.Register<RequestTransitionHistoryResponse>(app: "digital-processes");
    return registry;
});

builder.Services.AddSingleton<IApplicationNavigationRegistry>(sp =>
{
    var registry = new ApplicationNavigationRegistry();
    registry.RegisterArchitecture();
    registry.RegisterDigitalProcesses();
    return registry;
});

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
builder.Services.AddScoped<IDatabaseDumpService, DatabaseDumpService>();
builder.Services.AddHostedService<DatabaseDumpHostedService>();
builder.Services.AddScoped<ISecurityProjectionService, SecurityProjectionService>();
builder.Services.AddScoped<IRbacAccessBindingManager, RbacAccessBindingManager>();
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

builder.Services.AddScoped<IFormDefinitionService, FormDefinitionService>();
builder.Services.AddScoped<IFormVersionService, FormVersionService>();
builder.Services.AddScoped<IFormNodeTypeService, FormNodeTypeService>();
builder.Services.AddScoped<IFormNodeService, FormNodeService>();
builder.Services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
builder.Services.AddScoped<IWorkflowVersionService, WorkflowVersionService>();
builder.Services.AddScoped<IWorkflowStateService, WorkflowStateService>();
builder.Services.AddScoped<IWorkflowStateAccessRuleService, WorkflowStateAccessRuleService>();
builder.Services.AddScoped<IWorkflowTransitionService, WorkflowTransitionService>();
builder.Services.AddScoped<IProcessDefinitionService, ProcessDefinitionService>();
builder.Services.AddScoped<IProcessDefinitionVersionService, ProcessDefinitionVersionService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<ISubmissionNodeService, SubmissionNodeService>();
builder.Services.AddScoped<IRequestTransitionHistoryService, RequestTransitionHistoryService>();
builder.Services.AddSingleton<IDigitalProcessIntegration, UnconfiguredDigitalProcessIntegration>();
builder.Services.AddDigitalProcessRenderers();
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
    await db.Database.EnsureCreatedAsync();

    var dumpPath = builder.Configuration["DatabaseDump:Path"]
        ?? Environment.GetEnvironmentVariable("DATABASE_DUMP_PATH")
        ?? "./data/dump.json";
    var dumpService = scope.ServiceProvider.GetRequiredService<IDatabaseDumpService>();
    var restoredFromDump = await dumpService.LoadIfExistsAsync(dumpPath);

    if (restoredFromDump)
    {
        // A dump is a complete database snapshot. Never mix a restored snapshot with
        // the external SystemData/GitHub import path. Only local/idempotent bootstrap
        // reconciliation is allowed after restore.
        await DatabaseInitializer.InitializeAsync(
            db,
            systemDataSource: null,
            importSystemData: false);
    }
    else
    {
        var systemDataSource = builder.Configuration["SystemData:Source"]
            ?? Environment.GetEnvironmentVariable("SYSTEMDATA_SOURCE")
            ?? SystemDataImporter.DefaultSource;
        var importSystemData = builder.Configuration.GetValue("SystemData:Import", true);
        await DatabaseInitializer.InitializeAsync(db, systemDataSource, importSystemData);
    }

    var securityProjection = scope.ServiceProvider.GetRequiredService<ISecurityProjectionService>();
    await securityProjection.RebuildAllAsync();
}

// ============================================================
// Swagger
// ============================================================

app.UseEasyEntraHeaderPrincipal();
app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/", HostResolvers.Home)
    .ApplyViewModel<ApplicationHomePage, ApplicationDefinition[]>()
    .WithName("ApplicationHome")
    .WithTags("Home")
    .WithDescription("""
        Displays the application home page.

        The page lists all registered application branches and provides navigation
        to the Architecture and Digital Processes application roots.
        """);

app.MapGet("/architecture", HostResolvers.Architecture)
    .ApplyViewModel<ApplicationDashboardPage, ApplicationDefinition>()
    .WithName("ArchitectureHome")
    .WithTags("Home")
    .WithDescription("""
        Displays the Architecture application landing page and its navigation entries.
        """);

app.MapGet("/digital-processes", HostResolvers.DigitalProcesses)
    .ApplyViewModel<ApplicationDashboardPage, ApplicationDefinition>()
    .WithName("DigitalProcessesHome")
    .WithTags("Home")
    .WithDescription("""
        Displays the Digital Processes application landing page.

        The page provides entry points to requests, process definitions, forms,
        workflows, submissions and Digital Processes administration.
        """);

app.MapGet("/info", HostResolvers.Info)
    .WithTags("Diagnostics")
    .WithDescription("""
        Returns basic application, authentication and storage diagnostics as JSON.
        """);

app.MapGet("/debug", HostResolvers.Debug)
    .WithTags("Diagnostics")
    .WithDescription("""
        Renders a diagnostic HTML page containing the current identity headers, claims and decoded Easy Auth principal.
        """);

// ============================================================
// TODO 3. Mapování endpointů z template
// ============================================================
app.MapUserEndpoints("/architecture");
app.MapGroupEndpoints("/architecture");
app.MapRoleEndpoints("/architecture");
app.MapMembershipEndpoints("/architecture");
app.MapRoleTypeEndpoints("/architecture");
app.MapSecurityEndpoints("/architecture");
app.MapDigitalProcessEndpoints("/digital-processes");

// ============================================================
// Start
// ============================================================

app.Run(
    "http://0.0.0.0:8080"
);
