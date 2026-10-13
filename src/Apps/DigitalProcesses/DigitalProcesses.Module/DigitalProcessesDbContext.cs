using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>
/// Application-specific EF model: Core tables plus DigitalProcesses tables.
/// The one registered instance also serves CoreDbContext through DI.
/// </summary>
public class DigitalProcessesDbContext : CoreDbContext
{
    public DigitalProcessesDbContext(DbContextOptions<DigitalProcessesDbContext> options)
        : base(options) { }

    protected DigitalProcessesDbContext(DbContextOptions options) : base(options) { }

    public DbSet<FormNodeApplicantAccessModel> FormNodeApplicantAccess => Set<FormNodeApplicantAccessModel>();

    public DbSet<DocumentTemplateModel> DocumentTemplates => Set<DocumentTemplateModel>();
    public DbSet<DocumentTemplateRevisionModel> DocumentTemplateRevisions => Set<DocumentTemplateRevisionModel>();
    public DbSet<TemplateFieldBindingModel> TemplateFieldBindings => Set<TemplateFieldBindingModel>();
    public DbSet<FormDefinitionModel> FormDefinitions => Set<FormDefinitionModel>();
    public DbSet<FormVersionModel> FormVersions => Set<FormVersionModel>();
    public DbSet<FormNodeTypeModel> FormNodeTypes => Set<FormNodeTypeModel>();
    public DbSet<FormNodeModel> FormNodes => Set<FormNodeModel>();
    public DbSet<WorkflowDefinitionModel> WorkflowDefinitions => Set<WorkflowDefinitionModel>();
    public DbSet<WorkflowVersionModel> WorkflowVersions => Set<WorkflowVersionModel>();
    public DbSet<WorkflowStateModel> WorkflowStates => Set<WorkflowStateModel>();
    public DbSet<WorkflowTransitionModel> WorkflowTransitions => Set<WorkflowTransitionModel>();
    public DbSet<ProcessDefinitionModel> ProcessDefinitions => Set<ProcessDefinitionModel>();
    public DbSet<ProcessDefinitionVersionModel> ProcessDefinitionVersions => Set<ProcessDefinitionVersionModel>();
    public DbSet<RequestModel> DigitalProcessRequests => Set<RequestModel>();
    public DbSet<SubmissionModel> Submissions => Set<SubmissionModel>();
    public DbSet<SubmissionNodeModel> SubmissionNodes => Set<SubmissionNodeModel>();
    public DbSet<RequestTransitionHistoryModel> RequestTransitionHistory => Set<RequestTransitionHistoryModel>();
    public DbSet<WorkflowStateAccessRuleModel> WorkflowStateAccessRules => Set<WorkflowStateAccessRuleModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureDigitalProcesses();
    }
}
