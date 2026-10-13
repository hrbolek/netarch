using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.AppComposition;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Hosting;

namespace WebAppArchitecture.Host;

/// <summary>Composition root for EF mappings contributed by installed applications.
/// Domain EF mappings are contributed through IModule.ConfigureDatabase.</summary>
public sealed class HostDbContext : CoreDbContext
{
    private readonly IReadOnlyList<IAppModelContributor> _contributors;
    private readonly IReadOnlyList<IModule> _modules;

    public HostDbContext(DbContextOptions<HostDbContext> options, IEnumerable<IAppModelContributor> contributors, IEnumerable<IModule> modules)
        : base(options)
    {
        _contributors = contributors.ToArray();
        _modules = modules.ToArray();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var contributor in _contributors)
            contributor.Configure(modelBuilder);
        foreach (var module in _modules)
            module.ConfigureDatabase(modelBuilder);
    }
}
