using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed class DigitalProcessesManagementService(ISecurityLayer security, IDBLayer db)
    : SecuredBaseService<ProcessDefinitionVersionModel>(security, db), IDigitalProcessesManagementService
{
    public async Task<ApplicantMatrixPageData?> ApplicantMatrixAsync(Guid id, CancellationToken ct)
    {
        var process = await Security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Read, ct);
        if (process is null) return null;
        var mayEdit = !process.is_published &&
            await Security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Update, ct) is not null;
        var states = Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Where(x => x.workflow_version_id == process.workflow_version_id)
            .OrderBy(x => x.position)
            .ToArray().Select(WorkflowStateResponse.FromModel).ToArray();
        // Preserve sibling order instead of sorting path strings lexically.
        var nodes = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == process.form_version_id)
            .ToArray().Select(FormNodeResponse.FromModel).ToArray();
        return new(ProcessDefinitionVersionResponse.FromModel(process), mayEdit, states, nodes);
    }

    public async Task<FormTemplatesPageData?> TemplatePageAsync(Guid id, CancellationToken ct)
    {
        var version = await Security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Read, ct);
        if (version is null) return null;
        var mayEdit = !version.is_published &&
            await Security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Update, ct) is not null;
        return new(FormVersionResponse.FromModel(version), mayEdit);
    }
}
