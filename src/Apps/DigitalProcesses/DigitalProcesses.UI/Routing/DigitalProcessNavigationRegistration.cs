using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.UI.Routing;

namespace WebAppArchitecture.DigitalProcesses.UI.Routing;

public static class DigitalProcessNavigationRegistration
{
    public static IApplicationNavigationRegistry RegisterDigitalProcesses(this IApplicationNavigationRegistry registry)
    {
        registry.Register(new ApplicationDefinition(
            Key: "digital-processes",
            Label: "Digital Processes",
            RootUrl: "/digital-processes",
            Description: "Requests, workflow definitions, forms, submissions and process administration.",
            Navigation:
            [
                new("Requests", Children:
                [
                    new("Requests", "/digital-processes/RequestResponse/list"),
                    new("New request", "/digital-processes/RequestResponse/create"),
                    new("Submissions", "/digital-processes/SubmissionResponse/list"),
                    new("Transition history", "/digital-processes/RequestTransitionHistoryResponse/list")
                ]),
                new("Processes", Children:
                [
                    new("Definitions", "/digital-processes/ProcessDefinitionResponse/list"),
                    new("New process", "/digital-processes/ProcessDefinitionResponse/create")
                ]),
                new("Forms", Children:
                [
                    new("Definitions", "/digital-processes/FormDefinitionResponse/list"),
                    new("New form", "/digital-processes/FormDefinitionResponse/create")
                ]),
                new("Workflows", Children:
                [
                    new("Definitions", "/digital-processes/WorkflowDefinitionResponse/list"),
                    new("New workflow", "/digital-processes/WorkflowDefinitionResponse/create")
                ]),
                new("Administration", Children:
                [
                    new("Form node types", "/digital-processes/FormNodeTypeResponse/list"),
                    new("New form node type", "/digital-processes/FormNodeTypeResponse/create")
                ])
            ]));

        return registry;
    }
}
