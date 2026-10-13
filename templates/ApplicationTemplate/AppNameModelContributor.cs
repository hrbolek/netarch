using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.AppComposition;
namespace RootNamespace.AppName;

public sealed class AppNameModelContributor : IAppModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FirstEntityModel>(e => {
            e.ToTable("appname_first_entities"); e.HasKey(x => x.id);
            e.HasIndex(x => x.rbacobject_id);
        });
        modelBuilder.Entity<SecondEntityModel>(e => {
            e.ToTable("appname_second_entities"); e.HasKey(x => x.id);
            e.HasIndex(x => x.first_entity_id);
            e.HasOne<FirstEntityModel>().WithMany().HasForeignKey(x => x.first_entity_id)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
