using Microsoft.EntityFrameworkCore;
namespace WebAppArchitecture.AppComposition;
/// <summary>Each application contributes only its EF mapping to the host-owned model.</summary>
public interface IAppModelContributor
{
    void Configure(ModelBuilder modelBuilder);
}
