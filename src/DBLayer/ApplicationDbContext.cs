using Microsoft.EntityFrameworkCore;

namespace WebAppArchitecture.DBLayer;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<UserModel> Users => Set<UserModel>();
    public DbSet<GroupModel> Groups => Set<GroupModel>();
    public DbSet<GroupTypeModel> GroupTypes => Set<GroupTypeModel>();
    public DbSet<RoleModel> Roles => Set<RoleModel>();
    public DbSet<MembershipModel> Memberships => Set<MembershipModel>();
    public DbSet<RoleTypeModel> RoleTypes => Set<RoleTypeModel>();
    public DbSet<OperationModel> Operations => Set<OperationModel>();
    public DbSet<RoleTypeOperationModel> RoleTypeOperations => Set<RoleTypeOperationModel>();
    public DbSet<EffectiveAccessModel> EffectiveAccesses => Set<EffectiveAccessModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>(e =>
        {
            e.ToTable("users"); e.HasKey(x => x.id);
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.email).HasColumnName("email");
            e.Property(x => x.surname).HasColumnName("surname");
            ConfigureSecured(e);
        });

        modelBuilder.Entity<GroupTypeModel>(e =>
        {
            e.ToTable("group_types"); e.HasKey(x => x.id);
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.parent_group_type_id).HasColumnName("parent_group_type_id");
            e.Property(x => x.path).HasColumnName("path").IsRequired();
            e.Ignore(x => x.parent_group_type_name);
            e.HasIndex(x => x.path);
            e.HasOne<GroupTypeModel>().WithMany().HasForeignKey(x => x.parent_group_type_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });

        modelBuilder.Entity<GroupModel>(e =>
        {
            e.ToTable("groups"); e.HasKey(x => x.id);
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.group_type_id).HasColumnName("group_type_id");
            e.Property(x => x.master_group_id).HasColumnName("master_group_id");
            e.Property(x => x.path).HasColumnName("path").IsRequired();
            e.Ignore(x => x.master_group_name);
            e.Ignore(x => x.group_type_name);
            e.HasIndex(x => x.path);
            e.HasIndex(x => x.group_type_id);
            e.HasOne<GroupModel>().WithMany().HasForeignKey(x => x.master_group_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<GroupTypeModel>().WithMany().HasForeignKey(x => x.group_type_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });

        modelBuilder.Entity<RoleTypeModel>(e =>
        {
            e.ToTable("role_types"); e.HasKey(x => x.id);
            e.HasIndex(x => x.name).IsUnique();
            e.Ignore(x => x.requested_operations);
            ConfigureSecured(e);
        });

        modelBuilder.Entity<OperationModel>(e =>
        {
            e.ToTable("operations"); e.HasKey(x => x.id);
            e.HasIndex(x => x.name).IsUnique();
        });

        modelBuilder.Entity<RoleTypeOperationModel>(e =>
        {
            e.ToTable("role_type_operations"); e.HasKey(x => x.id);
            e.HasIndex(x => new { x.role_type_id, x.operation_id }).IsUnique();
            e.HasIndex(x => new { x.operation_id, x.role_type_id });
            e.HasOne<RoleTypeModel>()
                .WithMany(x => x.operation_links)
                .HasForeignKey(x => x.role_type_id)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.operation)
                .WithMany()
                .HasForeignKey(x => x.operation_id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoleModel>(e =>
        {
            e.ToTable("roles"); e.HasKey(x => x.id);
            e.Ignore(x => x.group_name); e.Ignore(x => x.user_name);
            e.HasIndex(x => new { x.user_id, x.group_id });
            e.HasIndex(x => x.role_type_id);
            e.HasOne(x => x.role_type).WithMany().HasForeignKey(x => x.role_type_id).OnDelete(DeleteBehavior.Restrict);
            ConfigureSecured(e);
        });

        modelBuilder.Entity<MembershipModel>(e =>
        {
            e.ToTable("memberships"); e.HasKey(x => x.id);
            e.Ignore(x => x.group_name); e.Ignore(x => x.user_name);
            e.HasIndex(x => new { x.user_id, x.group_id });
            ConfigureSecured(e);
        });

        modelBuilder.Entity<EffectiveAccessModel>(e =>
        {
            e.ToTable("effective_access");
            e.HasKey(x => new { x.user_id, x.rbacobject_id, x.role_type_id, x.source_role_id, x.source_membership_id });

            // Security queries join secured entities by rbacobject_id first and then
            // constrain access for the current user and validity interval.
            // Keep both orderings: rbac-first is ideal for entity -> access joins,
            // user-first is useful for user-centric security lookups/batch enrichment.
            e.HasIndex(x => new { x.rbacobject_id, x.user_id, x.valid_from_unix_ms, x.valid_to_unix_ms, x.role_type_id });
            e.HasIndex(x => new { x.user_id, x.rbacobject_id, x.role_type_id });
            e.HasIndex(x => x.source_role_id);
            e.HasIndex(x => x.source_group_id);
            e.HasIndex(x => x.source_membership_id);
        });
    }

    private static void ConfigureSecured<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> e)
        where TEntity : class, WebAppArchitecture.Base.Data.ISecuredEntityDBModel
    {
        e.Property(x => x.rbacobject_id).HasColumnName("rbacobject_id");
        e.Property(x => x.created_at).HasColumnName("created_at");
        e.Property(x => x.updated_at).HasColumnName("updated_at");
        e.Property(x => x.created_by).HasColumnName("created_by");
        e.Property(x => x.updated_by).HasColumnName("updated_by");
    }
}
