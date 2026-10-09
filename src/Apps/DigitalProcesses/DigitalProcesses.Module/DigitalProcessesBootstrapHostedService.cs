using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses;

public sealed class DigitalProcessesBootstrapHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var actor = RbacWellKnown.BootstrapAdminUserId;
        var scopeId = RbacWellKnown.FormNodeTypeCreationScopeId;

        var seeds = new[]
        {
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.Section, null, "Section", "section", true, false, true, null, scopeId, now, now, actor, actor),
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.Text, null, "Single-line text", "input.text", false, true, true, null, scopeId, now, now, actor, actor),
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.TextArea, null, "Multi-line text", "input.textarea", false, true, true, null, scopeId, now, now, actor, actor),
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.Number, null, "Number", "input.number", false, true, true, null, scopeId, now, now, actor, actor),
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.Date, null, "Date", "input.date", false, true, true, null, scopeId, now, now, actor, actor),
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.Checkbox, null, "Checkbox", "input.checkbox", false, true, true, null, scopeId, now, now, actor, actor),
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.Import, null, "Import dat (GraphQL)", "action.import", false, false, false, null, scopeId, now, now, actor, actor),
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.Export, null, "Export dat (GraphQL)", "action.export", false, false, false, null, scopeId, now, now, actor, actor),
            new FormNodeTypeModel(DigitalProcessWellKnown.FormNodeTypes.Select, null, "Select", "input.select", false, true, true, null, scopeId, now, now, actor, actor)
        };

        var ids = seeds.Select(x => x.id).ToArray();
        var existing = await db.FormNodeTypes.Where(x => ids.Contains(x.id)).Select(x => x.id).ToArrayAsync(cancellationToken);
        var existingSet = existing.ToHashSet();
        foreach (var seed in seeds.Where(x => !existingSet.Contains(x.id)))
            db.FormNodeTypes.Add(seed);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
