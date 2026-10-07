using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.Group.Resolvers;
using WebAppArchitecture.Group.Services;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Resolvers;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Resolvers;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.RoleType.Models;
using WebAppArchitecture.RoleType.Resolvers;
using WebAppArchitecture.RoleType.Services;
using WebAppArchitecture.Security.Resolvers;
using WebAppArchitecture.Security.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Resolvers;
using WebAppArchitecture.User.Services;

namespace WebAppArchitecture.Hosting;

public sealed class CoreModule : IArchitectureModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var configuredConnectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=./data/application.db";

        var connectionString = configuredConnectionString.Contains("Default Timeout=", StringComparison.OrdinalIgnoreCase)
            ? configuredConnectionString
            : configuredConnectionString.TrimEnd(';') + ";Default Timeout=5;Foreign Keys=True";

        Directory.CreateDirectory("./data");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddHttpContextAccessor();
        services.AddScoped<IDBLayer, DBLayer>();
        services.AddScoped<IDatabaseDumpService, DatabaseDumpService>();
        services.AddHostedService<CoreDatabaseHostedService>();
        services.AddScoped<ISecurityProjectionService, SecurityProjectionService>();
        services.AddScoped<IRbacAccessBindingManager, RbacAccessBindingManager>();
        services.AddScoped<ISecurityLayer, SecurityLayer>();

        services.AddScoped<IUserStore, EfUserStore>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGroupStore, EfGroupStore>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IGroupTypeStore, EfGroupTypeStore>();
        services.AddScoped<IGroupTypeService, GroupTypeService>();
        services.AddScoped<IRoleStore, EfRoleStore>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IMembershipStore, EfMembershipStore>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IRoleTypeStore, EfRoleTypeStore>();
        services.AddScoped<IRoleTypeService, RoleTypeService>();

        services.AddScoped<IBaseStore<UserModel>>(sp => sp.GetRequiredService<IUserStore>());
        services.AddScoped<IBaseStore<GroupModel>>(sp => sp.GetRequiredService<IGroupStore>());
        services.AddScoped<IBaseStore<GroupTypeModel>>(sp => sp.GetRequiredService<IGroupTypeStore>());
        services.AddScoped<IBaseStore<RoleModel>>(sp => sp.GetRequiredService<IRoleStore>());
        services.AddScoped<IBaseStore<MembershipModel>>(sp => sp.GetRequiredService<IMembershipStore>());
        services.AddScoped<IBaseStore<RoleTypeModel>>(sp => sp.GetRequiredService<IRoleTypeStore>());

        services.AddSingleton<ICurrentActorProvider, HeaderOrClaimActorProvider>();
        services.AddScoped<SecurityService>();
        services.AddScoped<ISecurityService>(sp => sp.GetRequiredService<SecurityService>());
        services.AddScoped<ISecurityPermissionService, SecurityPermissionService>();
        services.AddScoped<IRbacService>(sp => sp.GetRequiredService<SecurityService>());
        services.AddScoped<IRbacAuthorizationService>(sp => sp.GetRequiredService<SecurityService>());
    }

    public void ConfigureNavigation(IApplicationNavigationRegistry navigation)
    {
        navigation.RegisterArchitecture();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var routes = endpoints.ServiceProvider.GetRequiredService<IEntityRouteRegistry>();
        routes.Register<GroupTypeResponse>(operation: "read");

        endpoints.MapGet("/", CoreHostResolvers.Home)
            .ApplyViewModel<ApplicationHomePage, ApplicationDefinition[]>()
            .WithName("ApplicationHome")
            .WithTags("Home")
            .WithDescription("""
                Displays the application home page using all application definitions
                contributed by the explicitly mapped architecture modules.
                """);

        endpoints.MapGet("/architecture", CoreHostResolvers.Architecture)
            .ApplyViewModel<ApplicationDashboardPage, ApplicationDefinition>()
            .WithName("ArchitectureHome")
            .WithTags("Home")
            .WithDescription("Displays the Architecture application landing page and its navigation entries.");

        endpoints.MapGet("/info", CoreHostResolvers.Info)
            .WithTags("Diagnostics")
            .WithDescription("Returns basic application, authentication and storage diagnostics as JSON.");

        endpoints.MapGet("/debug", CoreHostResolvers.Debug)
            .WithTags("Diagnostics")
            .WithDescription("Renders a diagnostic HTML page containing the current identity headers and claims.");

        endpoints.MapUserEndpoints("/architecture");
        endpoints.MapGroupEndpoints("/architecture");
        endpoints.MapRoleEndpoints("/architecture");
        endpoints.MapMembershipEndpoints("/architecture");
        endpoints.MapRoleTypeEndpoints("/architecture");
        endpoints.MapSecurityEndpoints("/architecture");
    }
}
