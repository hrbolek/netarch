using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAppArchitecture.Events;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Facilities;

public static class FacilitiesModelBuilder
{
    public static void ConfigureFacilities(this ModelBuilder builder)
    {
        builder.Entity<FacilityModel>(e =>
        {
            ConfigureEntity(e, "facilities");
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Code).IsRequired();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_facility_code", "length(trim(\"Code\")) > 0");
                t.HasCheckConstraint("ck_facility_capacity", "\"Capacity\" IS NULL OR \"Capacity\" >= 0");
            });
            e.Property(x => x.path).IsRequired();
            e.HasIndex(x => x.path);
            e.HasIndex(x => x.ParentId);
            e.HasIndex(x => x.rbacobject_id).IsUnique();
            e.HasOne<GroupModel>().WithMany().HasForeignKey(x => x.rbacobject_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FacilityModel>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FacilityTypeModel>().WithMany().HasForeignKey(x => x.FacilityTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<FacilityTypeModel>(e =>
        {
            ConfigureEntity(e, "facility_types");
            e.Property(x => x.Code).IsRequired();
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasMany(x => x.AllowedChildTypes).WithOne().HasForeignKey(x => x.ParentTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<FacilityTypeAllowedChildModel>(e =>
        {
            e.ToTable("facility_type_allowed_children");
            e.HasKey(x => new { x.ParentTypeId, x.ChildTypeId });
            e.HasOne<FacilityTypeModel>().WithMany().HasForeignKey(x => x.ChildTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ChildTypeId);
        });
        builder.Entity<ReservationModel>(e =>
        {
            ConfigureEntity(e, "facility_reservations");
            e.HasOne<FacilityModel>().WithMany().HasForeignKey(x => x.FacilityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EventModel>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.EventId);
            e.HasIndex(x => new { x.FacilityId, x.Status, x.StartsAt, x.EndsAt });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_facility_reservation_interval", "\"StartsAt\" < \"EndsAt\"");
                t.HasCheckConstraint("ck_facility_reservation_status", "\"Status\" IN ('requested','confirmed','rejected','cancelled')");
            });
        });
    }

    private static void ConfigureEntity<T>(EntityTypeBuilder<T> e, string table) where T : FacilityEntity
    {
        e.ToTable(table); e.HasKey(x => x.id);
        e.Property(x => x.updated_at).IsConcurrencyToken();
        e.HasIndex(x => x.rbacobject_id);
    }
}
