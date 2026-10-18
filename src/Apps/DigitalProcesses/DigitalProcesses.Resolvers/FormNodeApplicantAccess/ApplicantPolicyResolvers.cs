using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeApplicantAccess;
using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormNodeApplicantAccess;

public static class ApplicantPolicyResolvers
{
    public static IResult List(Guid processVersionId, Guid? stateId, IApplicantPolicyService policies)
        => policies.List(processVersionId, stateId).ToHttpResult();

    public static async Task<IResult> Set(Guid processVersionId,
        SetApplicantVisibilityRequest body, IApplicantPolicyService policies, CancellationToken ct)
        => (await policies.SetAsync(processVersionId, body, ct)).ToHttpResult();
}
