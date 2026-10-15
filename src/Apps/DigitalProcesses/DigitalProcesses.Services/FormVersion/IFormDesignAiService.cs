using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.FormVersion;
public interface IFormDesignAiService
{
    Task<AuxiliaryOperationResult> DesignAsync(FormDesignRequest request, CancellationToken ct);
    Task<AuxiliaryOperationResult> ApproveAsync(ApproveFormDesignRequest request, CancellationToken ct);
}
