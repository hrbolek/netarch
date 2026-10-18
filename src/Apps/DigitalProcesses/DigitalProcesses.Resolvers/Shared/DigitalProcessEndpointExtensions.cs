using WebAppArchitecture.DigitalProcesses.Resolvers.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Resolvers.FormNode;
using WebAppArchitecture.DigitalProcesses.Resolvers.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Resolvers.FormVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Resolvers.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.Request;
using WebAppArchitecture.DigitalProcesses.Resolvers.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Resolvers.Submission;
using WebAppArchitecture.DigitalProcesses.Resolvers.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.Resolvers.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Resolvers.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Resolvers.WorkflowStateAccessRule;
using WebAppArchitecture.DigitalProcesses.Resolvers.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Resolvers.WorkflowVersion;
using Microsoft.AspNetCore.Routing;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
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
