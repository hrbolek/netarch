using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class WorkflowDefinitionHttpTests
{
    private const string Prefix = "/digital-processes/WorkflowVersionResponse";
    private static WorkflowGraphDefinition Graph => new(
        [new("first", "First", is_initial: true), new("second", "Second", position: 1),
         new("third", "Third", is_terminal: true, position: 2)],
        [new("next", "first", "second", "Next"), new("finish", "second", "third", "Finish"),
         new("return", "third", "first", "Return", true, "true", "null")]);

    [Theory]
    [InlineData("Material", 18, 17)]
    [InlineData("ZSC", 9, 7)]
    public async Task ExampleFiles_AreAcceptedDirectlyForReplacementAndCreation(string name, int states, int transitions)
    {
        using var host = new IsolatedHost();
        var definition = await SeedDefinition(host);
        using var client = host.CreateClient();
        var source = JsonNode.Parse(await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, $"Workflow.{name}.json")))!.AsObject();
        source["workflow_definition_id"] = definition;
        source["version"] = 1;
        using var create = await Send(client, HttpMethod.Post, Prefix + "/create-contained", source);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var version = (await create.Content.ReadFromJsonAsync<WorkflowVersionResponse>())!;
        using var replace = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id, source);
        Assert.Equal(HttpStatusCode.OK, replace.StatusCode);
        var graph = (await replace.Content.ReadFromJsonAsync<WorkflowDesignerResponse>())!;
        Assert.Equal(states, graph.states.Count);
        Assert.Equal(transitions, graph.transitions.Count);
    }

    [Fact]
    public async Task CreateAndReplaceCompleteGraph_PreservesIds_AndDeletesTransitionsBeforeStates()
    {
        using var host = new IsolatedHost();
        var definition = await SeedDefinition(host);
        using var client = host.CreateClient();
        using var create = await Send(client, HttpMethod.Post, Prefix + "/create-contained",
            new CreateWorkflowVersionRequest(definition, 1, states: Graph.states, transitions: Graph.transitions));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var version = (await create.Content.ReadFromJsonAsync<WorkflowVersionResponse>())!;
        var before = await Read(client, version.id);
        Assert.Equal(3, before.states.Count);
        Assert.Equal(3, before.transitions.Count);
        var firstId = before.states.Single(x => x.key == "first").id;
        var secondId = before.states.Single(x => x.key == "second").id;
        var nextId = before.transitions.Single(x => x.key == "next").id;
        var replacement = new WorkflowGraphDefinition(
            [new("first", "Renamed"), new("second", "New initial", is_initial: true)],
            [new("next", "second", "first", "Reversed", true, "false", "true")]);
        using var update = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id, replacement);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var after = (await update.Content.ReadFromJsonAsync<WorkflowDesignerResponse>())!;
        Assert.Equal(2, after.states.Count);
        Assert.Single(after.transitions);
        Assert.Equal(firstId, after.states.Single(x => x.key == "first").id);
        Assert.Equal(secondId, after.states.Single(x => x.is_initial).id);
        var edge = Assert.Single(after.transitions);
        Assert.Equal(nextId, edge.id);
        Assert.Equal(secondId, edge.source_state_id);
        Assert.Equal(firstId, edge.target_state_id);
        Assert.True(edge.note_required);
        Assert.Equal("false", edge.condition_query);
        using var repeat = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id, replacement);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Equal(nextId, Assert.Single((await Read(client, version.id)).transitions).id);
        var rewire = new WorkflowGraphDefinition(
            [new("first", "First", is_initial: true), new("fourth", "Fourth", is_terminal: true)],
            [new("next", "first", "fourth", "Rewired")]);
        using var rewired = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id, rewire);
        Assert.Equal(HttpStatusCode.OK, rewired.StatusCode);
        var rewiredGraph = await Read(client, version.id);
        Assert.DoesNotContain(rewiredGraph.states, x => x.id == secondId);
        Assert.Equal(rewiredGraph.states.Single(x => x.key == "fourth").id,
            Assert.Single(rewiredGraph.transitions).target_state_id);
        using var clone = await Send(client, HttpMethod.Post, Prefix + "/create-contained",
            new CreateWorkflowVersionRequest(definition, 2));
        Assert.Equal(HttpStatusCode.OK, clone.StatusCode);
        var cloned = await clone.Content.ReadFromJsonAsync<WorkflowVersionResponse>();
        var clonedGraph = await Read(client, cloned!.id);
        Assert.Equal(2, clonedGraph.states.Count);
        Assert.NotEqual(nextId, Assert.Single(clonedGraph.transitions).id);
        using var clear = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id,
            new WorkflowGraphDefinition([], []));
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        var empty = await Read(client, version.id);
        Assert.Empty(empty.states);
        Assert.Empty(empty.transitions);
    }

    [Theory]
    [InlineData("duplicate-state")]
    [InlineData("duplicate-transition")]
    [InlineData("missing-state")]
    [InlineData("multiple-initial")]
    [InlineData("missing-array")]
    [InlineData("no-initial-publish")]
    public async Task InvalidGraph_IsRejectedWithoutCreatingVersion(string invalid)
    {
        using var host = new IsolatedHost();
        var definition = await SeedDefinition(host);
        using var client = host.CreateClient();
        var graph = invalid switch
        {
            "duplicate-state" => Graph with { states = [..Graph.states, Graph.states[0]] },
            "duplicate-transition" => Graph with { transitions = [..Graph.transitions, Graph.transitions[0]] },
            "missing-state" => Graph with { states = [Graph.states[0]] },
            "multiple-initial" => Graph with { states = [Graph.states[0], Graph.states[1] with { is_initial = true }] },
            "no-initial-publish" => Graph with { states = Graph.states.Select(x => x with { is_initial = false }).ToArray() },
            _ => Graph
        };
        using var response = await Send(client, HttpMethod.Post, Prefix + "/create-contained",
            new CreateWorkflowVersionRequest(definition, 1, publish: invalid == "no-initial-publish", states: graph.states,
                transitions: invalid == "missing-array" ? null : graph.transitions));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var verify = host.Services.CreateScope();
        Assert.False(await verify.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Set<WorkflowVersionModel>().AnyAsync(x => x.workflow_definition_id == definition));
    }

    [Fact]
    public async Task ReplacementCannotDeleteTransitionUsedInHistory()
    {
        using var host = new IsolatedHost();
        var definition = await SeedDefinition(host);
        using var client = host.CreateClient();
        using var create = await Send(client, HttpMethod.Post, Prefix + "/create-contained",
            new CreateWorkflowVersionRequest(definition, 1, states: Graph.states, transitions: Graph.transitions));
        var version = (await create.Content.ReadFromJsonAsync<WorkflowVersionResponse>())!;
        var before = await Read(client, version.id);
        var transition = before.transitions.Single(x => x.key == "return");
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var now = DateTimeOffset.UtcNow;
            var actor = RbacWellKnown.BootstrapAdminUserId;
            var rbac = RbacWellKnown.RootSecurityObjectId;
            var form = Guid.NewGuid(); var formVersion = Guid.NewGuid();
            var process = Guid.NewGuid(); var processVersion = Guid.NewGuid();
            var request = Guid.NewGuid(); var submission = Guid.NewGuid();
            var role = await db.RoleTypes.FirstAsync();
            db.Set<FormDefinitionModel>().Add(new(form, "History test", null, rbac, now, now, actor, actor));
            db.Set<FormVersionModel>().Add(new(formVersion, form, 1, false, null, rbac, now, now, actor, actor));
            db.Set<ProcessDefinitionModel>().Add(new(process, "History test", null, role.id, rbac, now, now, actor, actor));
            db.Set<ProcessDefinitionVersionModel>().Add(new(processVersion, process, formVersion, version.id,
                1, false, null, rbac, now, now, actor, actor));
            db.Set<RequestModel>().Add(new(request, processVersion, actor, transition.target_state_id,
                submission, "History test", rbac, now, now, actor, actor));
            db.Set<SubmissionModel>().Add(new(submission, request, formVersion, 1, true, now,
                rbac, now, now, actor, actor));
            db.Set<RequestTransitionHistoryModel>().Add(new(Guid.NewGuid(), request, transition.id,
                transition.source_state_id, transition.target_state_id, submission, actor, null, now,
                rbac, now, now, actor, actor));
            await db.SaveChangesAsync();
        }
        using var response = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id,
            Graph with { transitions = Graph.transitions.Where(x => x.key != "return").ToArray() });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(3, (await Read(client, version.id)).transitions.Count);
    }

    [Fact]
    public async Task PublishCompleteGraph_AndRejectSubsequentReplacement()
    {
        using var host = new IsolatedHost();
        var definition = await SeedDefinition(host);
        using var client = host.CreateClient();
        using var create = await Send(client, HttpMethod.Post, Prefix + "/create-contained",
            new CreateWorkflowVersionRequest(definition, 1, true, Graph.states, Graph.transitions));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var version = (await create.Content.ReadFromJsonAsync<WorkflowVersionResponse>())!;
        Assert.True(version.is_published);
        using var update = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id, Graph);
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
        using var denied = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id, Graph, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }

    [Fact]
    public async Task ReplacementRejectsReferencedState_AndInvalidGraphWithoutPartialUpdates()
    {
        using var host = new IsolatedHost();
        var definition = await SeedDefinition(host);
        using var client = host.CreateClient();
        using var create = await Send(client, HttpMethod.Post, Prefix + "/create-contained",
            new CreateWorkflowVersionRequest(definition, 1, states: Graph.states, transitions: Graph.transitions));
        var version = (await create.Content.ReadFromJsonAsync<WorkflowVersionResponse>())!;
        var before = await Read(client, version.id);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var role = await db.RoleTypes.FirstAsync();
            var now = DateTimeOffset.UtcNow;
            var actor = RbacWellKnown.BootstrapAdminUserId;
            db.Set<WorkflowStateAccessRuleModel>().Add(new WorkflowStateAccessRuleModel(
                Guid.NewGuid(), before.states.Single(x => x.key == "third").id,
                role.id, role.id, "request", null, RbacWellKnown.RootSecurityObjectId,
                now, now, actor, actor));
            await db.SaveChangesAsync();
        }
        using var rejected = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id,
            new WorkflowGraphDefinition([new("first", "Must not change", is_initial: true)], []));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        var unchanged = await Read(client, version.id);
        Assert.Equal(3, unchanged.states.Count);
        Assert.Equal(3, unchanged.transitions.Count);
        Assert.Equal("First", unchanged.states.Single(x => x.key == "first").name);
        using var invalid = await Send(client, HttpMethod.Put, Prefix + "/definition/" + version.id,
            Graph with { transitions = [new("invalid", "absent", "first", "Invalid")] });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(3, (await Read(client, version.id)).transitions.Count);
    }

    private static async Task<Guid> SeedDefinition(IsolatedHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = RbacWellKnown.BootstrapAdminUserId;
        db.Set<WorkflowDefinitionModel>().Add(new(id, "Import test", null,
            RbacWellKnown.RootSecurityObjectId, now, now, actor, actor));
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<HttpResponseMessage> Send(
        HttpClient client, HttpMethod method, string uri, object body, Guid? actor = null)
    {
        using var request = ActingRequest.Get(uri, actor ?? RbacWellKnown.BootstrapAdminUserId);
        request.Method = method;
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task<WorkflowDesignerResponse> Read(HttpClient client, Guid id)
    {
        using var request = ActingRequest.Get(Prefix + "/designer/" + id, RbacWellKnown.BootstrapAdminUserId);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowDesignerResponse>())!;
    }
}
