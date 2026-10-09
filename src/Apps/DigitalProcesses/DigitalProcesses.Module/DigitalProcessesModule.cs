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

namespace WebAppArchitecture.DigitalProcesses;

public sealed class DigitalProcessesModule : IArchitectureModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<PreviewFormInstanceStore>();
        services.AddScoped<IFormDefinitionService, FormDefinitionService>();
        services.AddScoped<IFormVersionService, FormVersionService>();
        services.AddScoped<IFormNodeTypeService, FormNodeTypeService>();
        services.AddScoped<IFormNodeService, FormNodeService>();
        services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
        services.AddScoped<IWorkflowVersionService, WorkflowVersionService>();
        services.AddScoped<IWorkflowStateService, WorkflowStateService>();
        services.AddScoped<IWorkflowStateAccessRuleService, WorkflowStateAccessRuleService>();
        services.AddScoped<IWorkflowTransitionService, WorkflowTransitionService>();
        services.AddScoped<IProcessDefinitionService, ProcessDefinitionService>();
        services.AddScoped<IProcessDefinitionVersionService, ProcessDefinitionVersionService>();
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
        services.AddHostedService<DigitalProcessesBootstrapHostedService>();
        services.AddDigitalProcessRenderers();
    }

    public void ConfigureNavigation(IApplicationNavigationRegistry navigation)
    {
        navigation.RegisterDigitalProcesses();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var routes = endpoints.ServiceProvider.GetRequiredService<IEntityRouteRegistry>();
        routes.Register<FormDefinitionResponse>(app: "digital-processes");
        routes.Register<FormVersionResponse>(app: "digital-processes");
        routes.Register<FormNodeTypeResponse>(app: "digital-processes");
        routes.Register<FormNodeResponse>(app: "digital-processes");
        routes.Register<WorkflowDefinitionResponse>(app: "digital-processes");
        routes.Register<WorkflowVersionResponse>(app: "digital-processes");
        routes.Register<WorkflowStateResponse>(app: "digital-processes");
        routes.Register<WorkflowStateAccessRuleResponse>(app: "digital-processes");
        routes.Register<WorkflowTransitionResponse>(app: "digital-processes");
        routes.Register<ProcessDefinitionResponse>(app: "digital-processes");
        routes.Register<ProcessDefinitionVersionResponse>(app: "digital-processes");
        routes.Register<RequestResponse>(app: "digital-processes");
        routes.Register<SubmissionResponse>(app: "digital-processes");
        routes.Register<SubmissionNodeResponse>(app: "digital-processes");
        routes.Register<RequestTransitionHistoryResponse>(app: "digital-processes");

        endpoints.MapGet("/digital-processes", DigitalProcessesModuleResolvers.Home)
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
        PreviewFormEndpoints.Map(endpoints);

        endpoints.MapDigitalProcessEndpoints("/digital-processes");
    }
}
