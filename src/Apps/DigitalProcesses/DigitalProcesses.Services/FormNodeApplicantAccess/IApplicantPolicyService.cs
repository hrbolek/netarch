using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNodeApplicantAccess;
public interface IApplicantPolicyService
{
    AuxiliaryOperationResult List(Guid processVersionId, Guid? stateId);
    Task<AuxiliaryOperationResult> SetAsync(Guid processVersionId, SetApplicantVisibilityRequest body, CancellationToken ct);
}
