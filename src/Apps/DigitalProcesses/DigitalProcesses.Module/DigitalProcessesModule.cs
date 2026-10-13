using WebAppArchitecture.DigitalProcesses.Security;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebAppArchitecture.DBLayer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Resolvers;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Routing;
using WebAppArchitecture.Hosting;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

public sealed class DigitalProcessesModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Override the Core-only context with one application context, not two contexts.
        // Both Core stores/security and DigitalProcesses share one EF change tracker
        // and transaction in the same request scope.
        // DbContext composition belongs to the executable Host. CoreDbContext
        // is registered by CoreModule; this module only contributes EF mappings.
        services.AddMemoryCache();
        services.AddHttpClient("digital-process-documents");
        services.AddHttpClient("digital-process-ai-agents");
        services.AddSingleton<PreviewFormInstanceStore>();
        services.AddScoped<RtfTemplateStore>();
        services.AddScoped<IFormDefinitionService, FormDefinitionService>();
        services.AddScoped<IFormVersionService, FormVersionService>();
        services.AddScoped<IFormNodeTypeService, FormNodeTypeService>();
        services.AddScoped<IFormNodeService, FormNodeService>();
        services.AddScoped<IBulkTreeService, BulkTreeService>();
        services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
        services.AddScoped<IWorkflowVersionService, WorkflowVersionService>();
        services.AddScoped<IWorkflowStateService, WorkflowStateService>();
        services.AddScoped<IWorkflowStateAccessRuleService, WorkflowStateAccessRuleService>();
        services.AddScoped<IWorkflowTransitionService, WorkflowTransitionService>();
        services.AddScoped<IProcessDefinitionService, ProcessDefinitionService>();
        services.AddScoped<IProcessDefinitionVersionService, ProcessDefinitionVersionService>();
        services.AddScoped<ITrustedSubmissionSnapshotReader, TrustedSubmissionSnapshotReader>();
        services.AddScoped<IProcessRequestProvisioner, ProcessRequestProvisioner>();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<ISubmissionNodeService, SubmissionNodeService>();
        services.AddScoped<IRequestTransitionHistoryService, RequestTransitionHistoryService>();
        services.AddSingleton<IDigitalProcessIntegration, UnconfiguredDigitalProcessIntegration>();
        services.AddHttpContextAccessor();
        services.AddSingleton(new FormDataBackendOptions
        {
            Endpoint = configuration["DigitalProcesses:DataBackend:Endpoint"],
            ForwardHeaders = configuration.GetSection("DigitalProcesses:DataBackend:ForwardHeaders").Get<string[]>() ?? Array.Empty<string>()
        });
        // Explicit provider selection: Development must be able to call the real JSONata container.
        // Dummy is the safe default when no external backend is configured.
        var provider = configuration["DigitalProcesses:DataBackend:Provider"] ?? "Dummy";
        if (string.Equals(provider, "GraphQL", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IFormDataBackend, GraphqlLikeFormDataBackend>();
        else if (string.Equals(provider, "Dummy", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IFormDataBackend, DummyJsonataCaller>();
        else
            throw new InvalidOperationException($"Unsupported DigitalProcesses:DataBackend:Provider '{provider}'. Expected GraphQL or Dummy.");
        services.AddScoped<IFormCalculationService, FormCalculationService>();
        services.AddScoped<IFormDataContextBuilder, FormDataContextBuilder>();
        services.AddSingleton<IFormExpressionEvaluator, FormExpressionEvaluator>();
        services.AddSingleton<IFormUrlTemplateRenderer, FormUrlTemplateRenderer>();
        services.AddHostedService<DigitalProcessesBootstrapHostedService>();
        services.AddDigitalProcessRenderers();
    }

    public void ConfigureDatabase(ModelBuilder modelBuilder)
        => modelBuilder.ConfigureDigitalProcesses();

    public void ConfigureNavigation(IApplicationNavigationRegistry navigation)
    {
        navigation.RegisterDigitalProcesses();
    }

    public void MapAPI(IEndpointRouteBuilder endpoints, string prefix)
        => MapEndpoints(endpoints, prefix);

    public void MapUI(IEndpointRouteBuilder endpoints, string prefix)
        => MapEndpoints(endpoints, prefix);

    private static void MapEndpoints(IEndpointRouteBuilder endpoints, string prefix)
    {
        var routes = endpoints.ServiceProvider.GetRequiredService<IEntityRouteRegistry>();
        routes.Register<FormDefinitionResponse>(app: prefix);
        routes.Register<FormVersionResponse>(app: prefix);
        routes.Register<FormNodeTypeResponse>(app: prefix);
        routes.Register<FormNodeResponse>(app: prefix);
        routes.Register<WorkflowDefinitionResponse>(app: prefix);
        routes.Register<WorkflowVersionResponse>(app: prefix);
        routes.Register<WorkflowStateResponse>(app: prefix);
        routes.Register<WorkflowStateAccessRuleResponse>(app: prefix);
        routes.Register<WorkflowTransitionResponse>(app: prefix);
        routes.Register<ProcessDefinitionResponse>(app: prefix);
        routes.Register<ProcessDefinitionVersionResponse>(app: prefix);
        routes.Register<RequestResponse>(app: prefix);
        routes.Register<SubmissionResponse>(app: prefix);
        routes.Register<SubmissionNodeResponse>(app: prefix);
        routes.Register<RequestTransitionHistoryResponse>(app: prefix);

        endpoints.MapGet("/" + prefix.Trim('/'), DigitalProcessesModuleResolvers.Home)
            .ApplyViewModel<ApplicationDashboardPage, ApplicationDefinition>()
            .WithName("DigitalProcessesHome")
            .WithTags("Home")
            .WithDescription("""
                Displays the Digital Processes application landing page.

                The page provides entry points to requests, process definitions, forms,
                workflows, submissions and Digital Processes administration.
                """);

        // Register preview routes in all environments so the designer never renders
        // an HTMX action whose target route is absent (404). Preview remains
        // experimental; protect access to the application until authorization
        // and persistent form instances have been implemented.
        var scoped = endpoints.MapGroup("/" + prefix.Trim('/'));
        PreviewFormEndpoints.Map(scoped);
        FormDocumentEndpoints.Map(scoped);
        RtfDocumentEndpoints.Map(scoped);
        TemplateManagementEndpoints.Map(scoped);
        FormComponentCatalogEndpoints.Map(scoped);
        FormDesignAiEndpoints.Map(scoped);
        DigitalProcessesManagementPages.Map(scoped);
        ApplicantPolicyEndpoints.Map(scoped);

        endpoints.MapDigitalProcessEndpoints("/" + prefix.Trim('/'));
    }
}
