using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Services.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
using WebAppArchitecture.DigitalProcesses.Services.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Services.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Services.Request;
using WebAppArchitecture.DigitalProcesses.Services.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Services.Shared;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using WebAppArchitecture.DigitalProcesses.Services.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowStateAccessRule;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.UI.FormNode.Renderers;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Routing;
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
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Routing;
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
        services.AddScoped<IRtfTemplateService, RtfTemplateService>();
        AddEntityService<FormDefinitionModel, IFormDefinitionService, FormDefinitionService>(services);
        AddEntityService<FormVersionModel, IFormVersionService, FormVersionService>(services);
        AddEntityService<FormNodeTypeModel, IFormNodeTypeService, FormNodeTypeService>(services);
        AddEntityService<FormNodeModel, IFormNodeService, FormNodeService>(services);
        AddEntityService<FormNodeApplicantAccessModel, IFormNodeApplicantAccessService, FormNodeApplicantAccessService>(services);
        services.AddScoped<IFormNodeInsertionService, FormNodeInsertionService>();
        services.AddScoped(typeof(IDigitalProcessActionService<>), typeof(DigitalProcessActionService<>));
        services.AddScoped<IFormSearchService, FormSearchService>();
        services.AddScoped<IFormComponentCatalogService, FormComponentCatalogService>();
        services.AddScoped<IFormDocumentService, FormDocumentService>();
        services.AddScoped<IApplicantPolicyService, ApplicantPolicyService>();
        services.AddScoped<IFormDesignAiService, FormDesignAiService>();
        AddEntityService<WorkflowDefinitionModel, IWorkflowDefinitionService, WorkflowDefinitionService>(services);
        AddEntityService<WorkflowVersionModel, IWorkflowVersionService, WorkflowVersionService>(services);
        AddEntityService<WorkflowStateModel, IWorkflowStateService, WorkflowStateService>(services);
        AddEntityService<WorkflowStateAccessRuleModel, IWorkflowStateAccessRuleService, WorkflowStateAccessRuleService>(services);
        AddEntityService<WorkflowTransitionModel, IWorkflowTransitionService, WorkflowTransitionService>(services);
        AddEntityService<ProcessDefinitionModel, IProcessDefinitionService, ProcessDefinitionService>(services);
        AddEntityService<ProcessDefinitionVersionModel, IProcessDefinitionVersionService, ProcessDefinitionVersionService>(services);
        services.AddScoped<ITrustedSubmissionSnapshotReader, TrustedSubmissionSnapshotReader>();
        services.AddScoped<IProcessRequestProvisioner, ProcessRequestProvisioner>();
        AddEntityService<RequestModel, IRequestService, RequestService>(services);
        AddEntityService<SubmissionModel, ISubmissionService, SubmissionService>(services);
        AddEntityService<SubmissionNodeModel, ISubmissionNodeService, SubmissionNodeService>(services);
        AddEntityService<RequestTransitionHistoryModel, IRequestTransitionHistoryService, RequestTransitionHistoryService>(services);
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

    private static void AddEntityService<TModel, TService, TImplementation>(IServiceCollection services)
        where TModel : class, ISecuredEntityDBModel
        where TService : class, IBaseService<TModel>
        where TImplementation : SecuredBaseService<TModel>, TService
    {
        services.AddScoped<TService, TImplementation>();
        services.AddScoped<IBaseService<TModel>>(provider => provider.GetRequiredService<TService>());
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

        // Temporary designer previews are an explicit architecture exception documented on
        // PreviewFormEndpoints. They check source-form access but are not durable, user-owned
        // Submissions. Keep routes available in all environments for designer HTMX actions;
        // this exception does not extend to the production endpoints registered below.
        var scoped = endpoints.MapGroup("/" + prefix.Trim('/'));
        PreviewFormEndpoints.Map(scoped);
        FormDocumentEndpoints.Map(scoped);
        RtfDocumentEndpoints.Map(scoped);
        TemplateManagementEndpoints.Map(scoped);
        FormComponentCatalogEndpoints.Map(scoped);
        FormSearchEndpoints.Map(scoped);
        FormDesignAiEndpoints.Map(scoped);
        DigitalProcessesManagementPages.Map(scoped);
        ApplicantPolicyEndpoints.Map(scoped);

        endpoints.MapDigitalProcessEndpoints("/" + prefix.Trim('/'));
    }
}
