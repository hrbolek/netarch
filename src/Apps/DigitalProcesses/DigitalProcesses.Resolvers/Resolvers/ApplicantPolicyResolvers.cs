using Microsoft.AspNetCore.Http;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class ApplicantPolicyResolvers
{
    public static IResult List(Guid processVersionId, Guid? stateId, IApplicantPolicyService policies)
        => policies.List(processVersionId, stateId).ToHttpResult();

    public static async Task<IResult> Set(Guid processVersionId,
        SetApplicantVisibilityRequest body, IApplicantPolicyService policies, CancellationToken ct)
        => (await policies.SetAsync(processVersionId, body, ct)).ToHttpResult();
}
