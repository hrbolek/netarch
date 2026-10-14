using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DigitalProcesses.Resolvers;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>Applicant visibility belongs to a draft process version; publishing freezes its rules.</summary>
public static class ApplicantPolicyEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/ProcessDefinitionVersionResponse/applicant-access")
            .WithTags("DigitalProcesses.ApplicantVisibility");
        group.MapGet("/{processVersionId:guid}", ApplicantPolicyResolvers.List).WithName("ApplicantAccessList")
            .WithDescription("Lists Applicant visibility rules for the process version. Optional query parameter stateId filters by state.");
        group.MapPut("/{processVersionId:guid}", ApplicantPolicyResolvers.Set).WithName("ApplicantAccessSet")
            .WithDescription("Sets Applicant visibility for a node in a workflow state of a draft process version. Pass stateId, formNodeId and canRead in the JSON body. Requires Update on the process version.");
    }
}
