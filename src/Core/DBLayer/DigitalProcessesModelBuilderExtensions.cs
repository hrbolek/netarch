using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

internal static class DigitalProcessesModelBuilderExtensions
{
    public static void ConfigureDigitalProcesses(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormDefinitionModel>(e => { e.ToTable("dp_form_definitions"); e.HasKey(x => x.id); e.HasIndex(x => x.name); ConfigureSecured(e); });
        modelBuilder.Entity<FormVersionModel>(e =>
        {
            e.ToTable("dp_form_versions"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.form_definition_id, x.version }).IsUnique();
            e.HasOne<FormDefinitionModel>().WithMany().HasForeignKey(x => x.form_definition_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });
        modelBuilder.Entity<FormNodeTypeModel>(e =>
        {
            e.ToTable("dp_form_node_types"); e.HasKey(x => x.id);
            e.HasIndex(x => x.name).IsUnique();
            e.HasOne<FormNodeTypeModel>().WithMany().HasForeignKey(x => x.parent_type_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });
        modelBuilder.Entity<FormNodeModel>(e =>
        {
            e.ToTable("dp_form_nodes"); e.HasKey(x => x.id);
            e.Property(x => x.path).IsRequired(); e.Property(x => x.key).IsRequired();
            e.HasIndex(x => x.path); e.HasIndex(x => new { x.form_version_id, x.parent_id, x.position });
            e.HasIndex(x => new { x.form_version_id, x.parent_id, x.key });
            e.HasOne<FormVersionModel>().WithMany().HasForeignKey(x => x.form_version_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FormNodeModel>().WithMany().HasForeignKey(x => x.parent_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FormNodeTypeModel>().WithMany().HasForeignKey(x => x.node_type_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });

        modelBuilder.Entity<WorkflowDefinitionModel>(e => { e.ToTable("dp_workflow_definitions"); e.HasKey(x => x.id); e.HasIndex(x => x.name); ConfigureSecured(e); });
        modelBuilder.Entity<WorkflowVersionModel>(e =>
        {
            e.ToTable("dp_workflow_versions"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.workflow_definition_id, x.version }).IsUnique();
            e.HasOne<WorkflowDefinitionModel>().WithMany().HasForeignKey(x => x.workflow_definition_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });
        modelBuilder.Entity<WorkflowStateModel>(e =>
        {
            e.ToTable("dp_workflow_states"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.workflow_version_id, x.key }).IsUnique();
            e.HasIndex(x => new { x.workflow_version_id, x.is_initial });
            e.HasOne<WorkflowVersionModel>().WithMany().HasForeignKey(x => x.workflow_version_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });
        modelBuilder.Entity<WorkflowTransitionModel>(e =>
        {
            e.ToTable("dp_workflow_transitions"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.workflow_version_id, x.key }).IsUnique();
            e.HasIndex(x => new { x.source_state_id, x.target_state_id });
            e.HasOne<WorkflowVersionModel>().WithMany().HasForeignKey(x => x.workflow_version_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkflowStateModel>().WithMany().HasForeignKey(x => x.source_state_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkflowStateModel>().WithMany().HasForeignKey(x => x.target_state_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });


        modelBuilder.Entity<FormNodeApplicantAccessModel>(e =>
        {
            e.ToTable("dp_form_node_applicant_access"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.process_definition_version_id, x.workflow_state_id, x.form_node_id }).IsUnique();
            e.HasOne<ProcessDefinitionVersionModel>().WithMany().HasForeignKey(x => x.process_definition_version_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkflowStateModel>().WithMany().HasForeignKey(x => x.workflow_state_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FormNodeModel>().WithMany().HasForeignKey(x => x.form_node_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });
        modelBuilder.Entity<WorkflowStateAccessRuleModel>(e =>
        {
            e.ToTable("dp_workflow_state_access_rules"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.workflow_state_id, x.source_role_type_id, x.target_scope, x.target_form_node_id });
            e.HasOne<WorkflowStateModel>().WithMany().HasForeignKey(x => x.workflow_state_id).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<RoleTypeModel>().WithMany().HasForeignKey(x => x.source_role_type_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RoleTypeModel>().WithMany().HasForeignKey(x => x.target_role_type_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FormNodeModel>().WithMany().HasForeignKey(x => x.target_form_node_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });

        modelBuilder.Entity<ProcessDefinitionModel>(e => { e.ToTable("dp_process_definitions"); e.HasKey(x => x.id); e.HasIndex(x => x.name); e.HasIndex(x => x.form_definition_id).IsUnique(); e.HasIndex(x => x.workflow_definition_id).IsUnique(); e.HasOne<FormDefinitionModel>().WithMany().HasForeignKey(x => x.form_definition_id).OnDelete(DeleteBehavior.Restrict); e.HasOne<WorkflowDefinitionModel>().WithMany().HasForeignKey(x => x.workflow_definition_id).OnDelete(DeleteBehavior.Restrict); e.HasOne<RoleTypeModel>().WithMany().HasForeignKey(x => x.initiator_role_type_id).OnDelete(DeleteBehavior.Restrict); ConfigureSecured(e); });
        modelBuilder.Entity<ProcessDefinitionVersionModel>(e =>
        {
            e.ToTable("dp_process_definition_versions"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.process_definition_id, x.version }).IsUnique();
            e.HasOne<ProcessDefinitionModel>().WithMany().HasForeignKey(x => x.process_definition_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FormVersionModel>().WithMany().HasForeignKey(x => x.form_version_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkflowVersionModel>().WithMany().HasForeignKey(x => x.workflow_version_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });

        modelBuilder.Entity<RequestModel>(e =>
        {
            e.ToTable("dp_requests"); e.HasKey(x => x.id);
            e.HasIndex(x => x.process_definition_version_id); e.HasIndex(x => x.initiator_user_id); e.HasIndex(x => x.current_state_id); e.HasIndex(x => x.current_submission_id);
            e.HasOne<ProcessDefinitionVersionModel>().WithMany().HasForeignKey(x => x.process_definition_version_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserModel>().WithMany().HasForeignKey(x => x.initiator_user_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkflowStateModel>().WithMany().HasForeignKey(x => x.current_state_id).OnDelete(DeleteBehavior.Restrict);
            // current_submission_id intentionally has no FK here to avoid a Request <-> Submission cycle in EF model creation.
            ConfigureSecured(e);
        });
        modelBuilder.Entity<SubmissionModel>(e =>
        {
            e.ToTable("dp_submissions"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.request_id, x.version }).IsUnique(); e.HasIndex(x => new { x.request_id, x.is_archived });
            e.HasOne<RequestModel>().WithMany().HasForeignKey(x => x.request_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FormVersionModel>().WithMany().HasForeignKey(x => x.form_version_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });
        modelBuilder.Entity<SubmissionNodeModel>(e =>
        {
            e.ToTable("dp_submission_nodes"); e.HasKey(x => x.id);
            e.Property(x => x.path).IsRequired();
            e.HasIndex(x => x.path); e.HasIndex(x => new { x.submission_id, x.parent_id, x.position });
            e.HasIndex(x => new { x.submission_id, x.parent_id, x.form_node_id, x.occurrence }).IsUnique();
            e.HasOne<SubmissionModel>().WithMany().HasForeignKey(x => x.submission_id).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<FormNodeModel>().WithMany().HasForeignKey(x => x.form_node_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SubmissionNodeModel>().WithMany().HasForeignKey(x => x.parent_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });
        modelBuilder.Entity<RequestTransitionHistoryModel>(e =>
        {
            e.ToTable("dp_request_transition_history"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.request_id, x.changed_at });
            e.HasOne<RequestModel>().WithMany().HasForeignKey(x => x.request_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkflowTransitionModel>().WithMany().HasForeignKey(x => x.transition_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkflowStateModel>().WithMany().HasForeignKey(x => x.from_state_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkflowStateModel>().WithMany().HasForeignKey(x => x.to_state_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SubmissionModel>().WithMany().HasForeignKey(x => x.archived_submission_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });
    }

    private static void ConfigureSecured<TEntity>(EntityTypeBuilder<TEntity> e)
        where TEntity : class, ISecuredEntityDBModel
    {
        e.Property(x => x.rbacobject_id).HasColumnName("rbacobject_id");
        e.Property(x => x.created_at).HasColumnName("created_at");
        e.Property(x => x.updated_at).HasColumnName("updated_at");
        e.Property(x => x.created_by).HasColumnName("created_by");
        e.Property(x => x.updated_by).HasColumnName("updated_by");
    }
}
