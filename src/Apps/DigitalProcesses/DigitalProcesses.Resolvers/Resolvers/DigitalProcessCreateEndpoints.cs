using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

internal static class DigitalProcessCreateEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        MapFormDefinition(endpoints, prefix);
        MapFormVersion(endpoints, prefix);
        MapFormNodeType(endpoints, prefix);
        MapFormNode(endpoints, prefix);
        MapWorkflowDefinition(endpoints, prefix);
        MapWorkflowVersion(endpoints, prefix);
        MapWorkflowState(endpoints, prefix);
        MapWorkflowStateAccessRule(endpoints, prefix);
        MapWorkflowTransition(endpoints, prefix);
        MapProcessDefinition(endpoints, prefix);
        MapProcessDefinitionVersion(endpoints, prefix);
        MapRequest(endpoints, prefix);
    }

    private static void MapFormDefinition(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormDefinitionResponse");
        group.MapGet("/create", () => Page(
            "Create form definition",
            prefix + "/FormDefinitionResponse/create-form",
            prefix + "/FormDefinitionResponse/list",
            "Creates the stable identity of a form. Form structure is edited in form versions.",
            Text("name", "Name"),
            TextArea("description", "Description", required: false),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a form definition.
                """);

        group.MapPost("/create-form", async (HttpContext http, IFormDefinitionService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateFormDefinitionRequest(Required(f, "name"), Optional(f, "description"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/FormDefinitionResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a form definition from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapFormVersion(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormVersionResponse");
        group.MapGet("/create", () => Page(
            "Create form version",
            prefix + "/FormVersionResponse/create-form",
            prefix + "/FormVersionResponse/list",
            "Creates a draft version of an existing form definition.",
            GuidField("form_definition_id", "Form definition ID"),
            Number("version", "Version", "1", min: "1"),
            Check("publish", "Publish immediately"),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a form version.
                """);

        group.MapPost("/create-form", async (HttpContext http, IFormVersionService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateFormVersionRequest(GuidValue(f, "form_definition_id"), IntValue(f, "version"), BoolValue(f, "publish"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/FormVersionResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a form version from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapFormNodeType(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormNodeTypeResponse");
        group.MapGet("/create", () => Page(
            "Create form node type",
            prefix + "/FormNodeTypeResponse/create-form",
            prefix + "/FormNodeTypeResponse/list",
            "Defines form-node capabilities and the renderer key used by the Razor renderer registry.",
            Text("name", "Name"),
            Text("renderer_key", "Renderer key", help: "Examples: section, input.text, input.number, input.date, input.checkbox, action.button"),
            Check("can_have_children", "Can have children"),
            Check("can_have_value", "Can have value"),
            Check("can_repeat", "Can repeat"),
            GuidField("parent_type_id", "Parent type ID", required: false),
            TextArea("configuration_schema_json", "Configuration schema JSON", required: false),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a form-node type.
                """);

        group.MapPost("/create-form", async (HttpContext http, IFormNodeTypeService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateFormNodeTypeRequest(
                Required(f, "name"), Required(f, "renderer_key"), BoolValue(f, "can_have_children"), BoolValue(f, "can_have_value"), BoolValue(f, "can_repeat"),
                OptionalGuid(f, "parent_type_id"), Optional(f, "configuration_schema_json"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/FormNodeTypeResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a form-node type from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapFormNode(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormNodeResponse");
        group.MapGet("/create", () => Page(
            "Create form node",
            prefix + "/FormNodeResponse/create-form",
            prefix + "/FormNodeResponse/list",
            "Adds a node to a draft form-version tree. The materialized path is computed by the service.",
            GuidField("form_version_id", "Form version ID"),
            Text("key", "Key"),
            GuidField("node_type_id", "Node type ID"),
            GuidField("parent_id", "Parent node ID", required: false),
            Text("label", "Label", required: false),
            TextArea("description", "Description", required: false),
            Number("position", "Position", "0", min: "0"),
            Number("min_occurs", "Minimum occurrences", "1", min: "0"),
            Number("max_occurs", "Maximum occurrences", "1", min: "1"),
            Check("own_security_scope", "Own RBAC security scope"),
            TextArea("configuration_json", "Configuration JSON", required: false),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to add a node to a draft form-version tree.
                """);

        group.MapPost("/create-form", async (HttpContext http, IFormNodeService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateNodeAsync(new CreateFormNodeRequest(
                GuidValue(f, "form_version_id"), Required(f, "key"), GuidValue(f, "node_type_id"), OptionalGuid(f, "parent_id"),
                Optional(f, "label"), Optional(f, "description"), IntValue(f, "position"), IntValue(f, "min_occurs"), IntValue(f, "max_occurs"),
                BoolValue(f, "own_security_scope"), Optional(f, "configuration_json"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/FormNodeResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a form node from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapWorkflowDefinition(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowDefinitionResponse");
        group.MapGet("/create", () => Page(
            "Create workflow definition",
            prefix + "/WorkflowDefinitionResponse/create-form",
            prefix + "/WorkflowDefinitionResponse/list",
            "Creates the stable identity of a workflow.",
            Text("name", "Name"),
            TextArea("description", "Description", required: false),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a workflow definition.
                """);

        group.MapPost("/create-form", async (HttpContext http, IWorkflowDefinitionService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateWorkflowDefinitionRequest(Required(f, "name"), Optional(f, "description"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/WorkflowDefinitionResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a workflow definition from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapWorkflowVersion(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowVersionResponse");
        group.MapGet("/create", () => Page(
            "Create workflow version",
            prefix + "/WorkflowVersionResponse/create-form",
            prefix + "/WorkflowVersionResponse/list",
            "Creates a draft version of an existing workflow definition.",
            GuidField("workflow_definition_id", "Workflow definition ID"),
            Number("version", "Version", "1", min: "1"),
            Check("publish", "Publish immediately"),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a workflow version.
                """);

        group.MapPost("/create-form", async (HttpContext http, IWorkflowVersionService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateWorkflowVersionRequest(GuidValue(f, "workflow_definition_id"), IntValue(f, "version"), BoolValue(f, "publish"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/WorkflowVersionResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a workflow version from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapWorkflowState(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowStateResponse");
        group.MapGet("/create", () => Page(
            "Create workflow state",
            prefix + "/WorkflowStateResponse/create-form",
            prefix + "/WorkflowStateResponse/list",
            "Adds a state to a draft workflow version.",
            GuidField("workflow_version_id", "Workflow version ID"),
            Text("key", "Key"),
            Text("name", "Name"),
            TextArea("description", "Description", required: false),
            Check("is_initial", "Initial state"),
            Check("is_terminal", "Terminal state"),
            Number("position", "Position", "0", min: "0"),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a workflow state.
                """);

        group.MapPost("/create-form", async (HttpContext http, IWorkflowStateService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateWorkflowStateRequest(
                GuidValue(f, "workflow_version_id"), Required(f, "key"), Required(f, "name"), Optional(f, "description"),
                BoolValue(f, "is_initial"), BoolValue(f, "is_terminal"), IntValue(f, "position"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/WorkflowStateResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a workflow state from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapWorkflowStateAccessRule(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowStateAccessRuleResponse");
        group.MapGet("/create", () => Page(
            "Create workflow state access rule",
            prefix + "/WorkflowStateAccessRuleResponse/create-form",
            prefix + "/WorkflowStateAccessRuleResponse/list",
            "Defines how a functional RBAC role is projected to the request, submission or a form-node scope when the state becomes active.",
            GuidField("workflow_state_id", "Workflow state ID"),
            GuidField("source_role_type_id", "Source role type ID"),
            GuidField("target_role_type_id", "Target role type ID"),
            Text("target_scope", "Target scope", value: "submission", help: "request, submission, or form-node"),
            GuidField("target_form_node_id", "Target form node ID", required: false),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a workflow-state RBAC access rule.
                """);

        group.MapPost("/create-form", async (HttpContext http, IWorkflowStateAccessRuleService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateWorkflowStateAccessRuleRequest(
                GuidValue(f, "workflow_state_id"), GuidValue(f, "source_role_type_id"), GuidValue(f, "target_role_type_id"),
                Required(f, "target_scope"), OptionalGuid(f, "target_form_node_id"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/WorkflowStateAccessRuleResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a workflow-state RBAC access rule from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapWorkflowTransition(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowTransitionResponse");
        group.MapGet("/create", () => Page(
            "Create workflow transition",
            prefix + "/WorkflowTransitionResponse/create-form",
            prefix + "/WorkflowTransitionResponse/list",
            "Adds a directed transition between two states of a draft workflow version.",
            GuidField("workflow_version_id", "Workflow version ID"),
            GuidField("source_state_id", "Source state ID"),
            GuidField("target_state_id", "Target state ID"),
            Text("key", "Key"),
            Text("name", "Name"),
            Check("note_required", "Transition note required"),
            TextArea("condition_query", "Condition query", required: false),
            TextArea("action_query", "Action query", required: false),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a workflow transition.
                """);

        group.MapPost("/create-form", async (HttpContext http, IWorkflowTransitionService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateWorkflowTransitionRequest(
                GuidValue(f, "workflow_version_id"), GuidValue(f, "source_state_id"), GuidValue(f, "target_state_id"),
                Required(f, "key"), Required(f, "name"), BoolValue(f, "note_required"), Optional(f, "condition_query"), Optional(f, "action_query"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/WorkflowTransitionResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a workflow transition from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapProcessDefinition(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/ProcessDefinitionResponse");
        group.MapGet("/create", () => Page(
            "Create process definition",
            prefix + "/ProcessDefinitionResponse/create-form",
            prefix + "/ProcessDefinitionResponse/list",
            "Creates a digital-process definition and selects the role type assigned to request initiators.",
            Text("name", "Name"),
            GuidField("initiator_role_type_id", "Initiator role type ID"),
            GuidField("rbacobject_id", "Process RBAC root group ID", help: "Required. Must reference the RBAC Group that is the security root of this process."),
            TextArea("description", "Description", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a process definition.
                """);

        group.MapPost("/create-form", async (HttpContext http, IProcessDefinitionService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateProcessDefinitionRequest(
                Required(f, "name"), GuidValue(f, "initiator_role_type_id"), Optional(f, "description"), GuidValue(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/ProcessDefinitionResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a process definition from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapProcessDefinitionVersion(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/ProcessDefinitionVersionResponse");
        group.MapGet("/create", () => Page(
            "Create process definition version",
            prefix + "/ProcessDefinitionVersionResponse/create-form",
            prefix + "/ProcessDefinitionVersionResponse/list",
            "Connects one form version with one workflow version in a versioned process definition.",
            GuidField("process_definition_id", "Process definition ID"),
            GuidField("form_version_id", "Form version ID"),
            GuidField("workflow_version_id", "Workflow version ID"),
            Number("version", "Version", "1", min: "1"),
            Check("publish", "Publish immediately"),
            GuidField("rbacobject_id", "RBAC object ID", required: false)))
            .WithDescription("""
                Renders the HTML form used to create a process-definition version.
                """);

        group.MapPost("/create-form", async (HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateAsync(new CreateProcessDefinitionVersionRequest(
                GuidValue(f, "process_definition_id"), GuidValue(f, "form_version_id"), GuidValue(f, "workflow_version_id"),
                IntValue(f, "version"), BoolValue(f, "publish"), OptionalGuid(f, "rbacobject_id")), ct);
            return Results.Redirect(prefix + "/ProcessDefinitionVersionResponse/view/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Creates a process-definition version from an HTML form submission and redirects to its detail page.
            """);
    }

    private static void MapRequest(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/RequestResponse");
        group.MapGet("/create", () => Page(
            "Create request",
            prefix + "/RequestResponse/create-form",
            prefix + "/RequestResponse/list",
            "Starts a request from a published process-definition version. The current user becomes the immutable initiator.",
            GuidField("process_definition_version_id", "Process definition version ID"),
            Text("subject", "Subject", required: false)))
            .WithDescription("""
                Renders the HTML form used to start a digital-process request.
                """);

        group.MapPost("/create-form", async (HttpContext http, IRequestService service, CancellationToken ct) =>
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.StartAsync(new StartRequestRequest(GuidValue(f, "process_definition_version_id"), Optional(f, "subject")), ct);
            return Results.Redirect(prefix + "/RequestResponse/detail/" + created.id);
        }).DisableAntiforgery().WithDescription("""
            Starts a request from an HTML form submission and redirects to its request detail page.
            """);
    }

    private static RazorComponentResult<DigitalProcessCreatePage> Page(
        string title,
        string action,
        string listUrl,
        string description,
        params DigitalProcessCreateField[] fields)
        => new(new Dictionary<string, object?>
        {
            [nameof(DigitalProcessCreatePage.Model)] = new DigitalProcessCreatePageModel(title, action, listUrl, fields, description)
        });

    private static DigitalProcessCreateField Text(string name, string label, bool required = true, string? value = null, string? help = null)
        => new(name, label, "text", required, help, value);

    private static DigitalProcessCreateField GuidField(string name, string label, bool required = true, string? help = null)
        => new(name, label, "text", required, help ?? "GUID identifier.");

    private static DigitalProcessCreateField TextArea(string name, string label, bool required = true)
        => new(name, label, "textarea", required);

    private static DigitalProcessCreateField Number(string name, string label, string value, string? min = null, string? max = null)
        => new(name, label, "number", true, null, value, false, min, max);

    private static DigitalProcessCreateField Check(string name, string label)
        => new(name, label, "checkbox", false);

    private static string Required(IFormCollection form, string name)
    {
        var value = form[name].ToString().Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new BadHttpRequestException($"Field '{name}' is required.");
        return value;
    }

    private static string? Optional(IFormCollection form, string name)
    {
        var value = form[name].ToString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static Guid GuidValue(IFormCollection form, string name)
        => Guid.TryParse(Required(form, name), out var value)
            ? value
            : throw new BadHttpRequestException($"Field '{name}' must be a GUID.");

    private static Guid? OptionalGuid(IFormCollection form, string name)
    {
        var raw = Optional(form, name);
        if (raw is null) return null;
        return Guid.TryParse(raw, out var value)
            ? value
            : throw new BadHttpRequestException($"Field '{name}' must be a GUID.");
    }

    private static int IntValue(IFormCollection form, string name)
        => int.TryParse(Required(form, name), out var value)
            ? value
            : throw new BadHttpRequestException($"Field '{name}' must be an integer.");

    private static bool BoolValue(IFormCollection form, string name)
        => string.Equals(form[name].ToString(), "true", StringComparison.OrdinalIgnoreCase)
           || string.Equals(form[name].ToString(), "on", StringComparison.OrdinalIgnoreCase);
}
