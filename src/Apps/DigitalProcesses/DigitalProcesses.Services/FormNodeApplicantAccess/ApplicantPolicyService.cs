using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Models.Shared;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNodeApplicantAccess;



public sealed class ApplicantPolicyService(IFormNodeApplicantAccessService rules) : IApplicantPolicyService
{
    public AuxiliaryOperationResult List(Guid processVersionId, Guid? stateId) => rules.List(processVersionId, stateId);
    public Task<AuxiliaryOperationResult> SetAsync(Guid processVersionId, SetApplicantVisibilityRequest body, CancellationToken ct) => rules.SetAsync(processVersionId, body, ct);
}
