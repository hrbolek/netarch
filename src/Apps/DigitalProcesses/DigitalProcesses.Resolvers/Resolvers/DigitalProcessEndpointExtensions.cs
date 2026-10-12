using Microsoft.AspNetCore.Routing;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

/// <summary>
/// Typ Digital Process Endpoint Extensions ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public static class DigitalProcessEndpointExtensions
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static IEndpointRouteBuilder MapDigitalProcessEndpoints(this IEndpointRouteBuilder endpoints, string prefix = "/digital-processes")
    {
        FormDefinitionResolvers.Map(endpoints, prefix);
        FormVersionResolvers.Map(endpoints, prefix);
        FormNodeTypeResolvers.Map(endpoints, prefix);
        FormNodeResolvers.Map(endpoints, prefix);
        WorkflowDefinitionResolvers.Map(endpoints, prefix);
        WorkflowVersionResolvers.Map(endpoints, prefix);
        WorkflowStateResolvers.Map(endpoints, prefix);
        WorkflowStateAccessRuleResolvers.Map(endpoints, prefix);
        WorkflowTransitionResolvers.Map(endpoints, prefix);
        ProcessDefinitionResolvers.Map(endpoints, prefix);
        ProcessDefinitionVersionResolvers.Map(endpoints, prefix);
        RequestResolvers.Map(endpoints, prefix);
        SubmissionResolvers.Map(endpoints, prefix);
        SubmissionNodeResolvers.Map(endpoints, prefix);
        RequestTransitionHistoryResolvers.Map(endpoints, prefix);
        return endpoints;
    }
}
