using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;
public interface IFormSearchService
{
    Task<AuxiliaryOperationResult> SearchAsync(FormSearchRequest request, CancellationToken ct);
    Task<AuxiliaryOperationResult> SearchDraftAsync(FormNodeDraftSearchRequest request, CancellationToken ct);
}
