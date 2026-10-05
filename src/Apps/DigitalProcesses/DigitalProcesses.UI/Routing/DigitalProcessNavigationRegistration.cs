using WebAppArchitecture.Base.UI.Routing;

namespace WebAppArchitecture.DigitalProcesses.UI.Routing;

public static class DigitalProcessNavigationRegistration
{
    public static ApplicationNavigationRegistry RegisterDigitalProcesses(this ApplicationNavigationRegistry registry)
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
                    new("New process", "/digital-processes/ProcessDefinitionResponse/create"),
                    new("Versions", "/digital-processes/ProcessDefinitionVersionResponse/list"),
                    new("New process version", "/digital-processes/ProcessDefinitionVersionResponse/create")
                ]),
                new("Forms", Children:
                [
                    new("Definitions", "/digital-processes/FormDefinitionResponse/list"),
                    new("New form", "/digital-processes/FormDefinitionResponse/create"),
                    new("Versions", "/digital-processes/FormVersionResponse/list"),
                    new("New form version", "/digital-processes/FormVersionResponse/create"),
                    new("Nodes", "/digital-processes/FormNodeResponse/list"),
                    new("New form node", "/digital-processes/FormNodeResponse/create")
                ]),
                new("Workflows", Children:
                [
                    new("Definitions", "/digital-processes/WorkflowDefinitionResponse/list"),
                    new("New workflow", "/digital-processes/WorkflowDefinitionResponse/create"),
                    new("Versions", "/digital-processes/WorkflowVersionResponse/list"),
                    new("New workflow version", "/digital-processes/WorkflowVersionResponse/create"),
                    new("States", "/digital-processes/WorkflowStateResponse/list"),
                    new("New state", "/digital-processes/WorkflowStateResponse/create"),
                    new("Transitions", "/digital-processes/WorkflowTransitionResponse/list"),
                    new("New transition", "/digital-processes/WorkflowTransitionResponse/create"),
                    new("Access rules", "/digital-processes/WorkflowStateAccessRuleResponse/list"),
                    new("New access rule", "/digital-processes/WorkflowStateAccessRuleResponse/create")
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
