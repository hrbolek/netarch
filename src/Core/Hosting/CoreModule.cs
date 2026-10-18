using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.DBLayer;
using DBLayerImplementation = WebAppArchitecture.DBLayer.DBLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.Group.Resolvers;
using WebAppArchitecture.Group.Services;
using WebAppArchitecture.GroupType.Models;
using WebAppArchitecture.GroupType.Resolvers;
using WebAppArchitecture.GroupType.Services;
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
using WebAppArchitecture.AuthorizationLayer;
using AuthorizationLayerImplementation = WebAppArchitecture.AuthorizationLayer.AuthorizationLayer;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Resolvers;
using WebAppArchitecture.User.Services;

namespace WebAppArchitecture.Hosting;

public sealed class CoreModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection (PostgreSQL).");
        // Core can run independently; application modules may replace this registration
        // with a derived context while retaining CoreDbContext as its scoped alias.
        services.AddDbContext<CoreDbContext>(options => options.UseNpgsql(connectionString));

        services.AddHttpContextAccessor();
        services.AddScoped<IDBLayer, DBLayerImplementation>();
        services.AddHostedService<CoreDatabaseHostedService>();
        services.AddHostedService<EffectiveAccessCleanupHostedService>();
        services.AddScoped<ISecurityProjectionService, SecurityProjectionService>();
        services.AddScoped<IRbacAccessBindingManager, RbacAccessBindingManager>();
        services.AddScoped<IAuthorizationLayer, AuthorizationLayerImplementation>();

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
        services.AddScoped<IRbacAuthorizationService, RbacAuthorizationService>();
        services.AddScoped<ISecurityPermissionService, SecurityPermissionService>();
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (DbUpdateConcurrencyException exception) when (!context.Response.HasStarted)
            {
                app.Logger.LogWarning(exception, "Concurrent update rejected for {Path}", context.Request.Path);
                await Results.Conflict(new { error = "The entity has changed. Reload it before updating." })
                    .ExecuteAsync(context);
            }
        });
        app.UseEasyEntraHeaderPrincipal();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    public void ConfigureNavigation(IApplicationNavigationRegistry navigation)
    {
        navigation.RegisterArchitecture();
    }

    public void MapAPI(IEndpointRouteBuilder endpoints, string prefix)
        => MapEndpoints(endpoints, prefix);

    public void MapUI(IEndpointRouteBuilder endpoints, string prefix)
        => MapEndpoints(endpoints, prefix);

    private static void MapEndpoints(IEndpointRouteBuilder endpoints, string prefix)
    {
        var routes = endpoints.ServiceProvider.GetRequiredService<IEntityRouteRegistry>();
        routes.Register<GroupTypeResponse>(app: prefix, operation: "read");
        routes.Register<UserResponse>(app: prefix);
        routes.Register<GroupResponse>(app: prefix);
        routes.Register<RoleResponse>(app: prefix);
        routes.Register<MembershipResponse>(app: prefix);
        routes.Register<RoleTypeResponse>(app: prefix);
        var root = "/" + prefix.Trim('/');
        endpoints.MapUserEndpoints(root);
        endpoints.MapGroupEndpoints(root);
        endpoints.MapGroupTypeEndpoints(root);
        endpoints.MapRoleEndpoints(root);
        endpoints.MapMembershipEndpoints(root);
        endpoints.MapRoleTypeEndpoints(root);
        endpoints.MapSecurityEndpoints(root);
    }
}
