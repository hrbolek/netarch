using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.UI.FormNode.Components;
using WebAppArchitecture.DigitalProcesses.UI.FormVersion.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormNode;
public static class FormNodeEditorResolvers
{
    public static async Task<object> Editor(Guid id, bool? test, IFormNodeService service, CancellationToken ct)
    {
        try { return await service.GetEditorAsync(id, test ?? false, ct); }
        catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
    }

    public static async Task<IResult> Library([AsParameters] FormNodeLibraryQuery query, IFormNodeInsertionService service, CancellationToken ct)
    {
        try { return Results.Ok(await service.SearchAsync(query, ct)); }
        catch (ArgumentException ex) { return Results.Problem(ex.Message, statusCode: 400); }
    }

    public static IReadOnlyList<FormRendererExample> Gallery(IFormNodeInsertionService service) => service.Examples();

    public static async Task<IResult> Prepare(PrepareFormNodeDraftRequest request, IFormNodeInsertionService service, CancellationToken ct)
    {
        try { return Results.Ok(await service.PrepareAsync(request, ct)); }
        catch (ArgumentException ex) { return Results.Problem(ex.Message, statusCode: 400); }
        catch (System.Text.Json.JsonException ex) { return Results.Problem(ex.Message, statusCode: 400); }
        catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
    }

    public static async Task<object> Workspace(FormNodeDraftWorkspaceRequest request, IFormNodeInsertionService service, CancellationToken ct)
    {
        try { return await service.WorkspaceAsync(request, ct); }
        catch (ArgumentException ex) { return Results.Problem(ex.Message, statusCode: 400); }
        catch (System.Text.Json.JsonException ex) { return Results.Problem(ex.Message, statusCode: 400); }
        catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
    }

    public static async Task<IResult> Insert(InsertFormNodeDraftRequest request, IFormNodeInsertionService service, CancellationToken ct)
    {
        try { return Results.Ok(await service.InsertAsync(request, ct)); }
        catch (ArgumentException ex) { return Results.Problem(ex.Message, statusCode: 400); }
        catch (System.Text.Json.JsonException ex) { return Results.Problem(ex.Message, statusCode: 400); }
        catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
        catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: 403); }
        catch (InvalidOperationException ex) { return Results.Problem(ex.Message, statusCode: 409); }
    }

    public static async Task<IResult> SearchDraft(FormNodeDraftSearchRequest request, IFormSearchService service, CancellationToken ct)
    {
        try { return (await service.SearchDraftAsync(request, ct)).ToHttpResult(); }
        catch (ArgumentException ex) { return Results.Problem(ex.Message, statusCode: 400); }
        catch (System.Text.Json.JsonException ex) { return Results.Problem(ex.Message, statusCode: 400); }
        catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
    }

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormNodeResponse").WithTags("FormNode");
        group.MapGet("/editor/{id:guid}", Editor).WithName("FormNodeEditor")
            .ApplyViewModel<FormVersionDesignerPage, FormDesignerPageResponse>()
            .WithDescription("Edits a readable stored node and its readable subtree using the existing form designer. Optional test=true selects preview. Original parent, identity and version are retained; mutations retain their own RBAC checks.")
            .ProducesProblem(404);
        group.MapGet("/library", Library).WithName("FormNodeLibrary")
            .Produces<FormNodeLibraryPage>().ProducesProblem(400)
            .WithDescription("RBAC-filtered node library; case-insensitive key/label/description and configuration text filters, exact renderer/type/source-version filters. Stable paging uses offset >= 0 and limit 1..50 (default 20). No source mutation.");
        group.MapGet("/renderer-examples", Gallery).WithName("FormNodeRendererExamples")
            .ApplyViewModel<FormNodeRendererGallery, IReadOnlyList<FormRendererExample>>()
            .WithDescription("Readable renderer types and existing built-in configuration examples; HTML uses the real registered Razor components, JSON returns example metadata.");
        group.MapPost("/draft", Prepare).WithName("FormNodePrepareDraft")
            .Produces<FormNodeDraft>().ProducesProblem(400).ProducesProblem(404)
            .WithDescription("Creates a detached client-owned draft from exactly one readable source node or renderer type. Optional include_children=false copies only the source node. New temporary IDs and remapped internal configuration references; no writes or server session.");
        group.MapPost("/draft/workspace", Workspace).WithName("FormNodeDraftWorkspace")
            .ApplyViewModel<FormNodeDraftWorkspace, FormNodeDraftWorkspaceResponse>()
            .WithDescription("Validates and renders a client-memory draft using shared node properties and real tree renderers. Requires Read on target version and draft types; no persisted changes or preview values in draft.");
        group.MapPost("/draft/preview", Workspace).WithName("FormNodeDraftPreview")
            .ApplyViewModel<FormNodeDraftPreview, FormNodeDraftWorkspaceResponse>()
            .WithDescription("Revalidates a draft and renders only its interactive preview. Read on target form required. Does not create Submission or save test values.");
        group.MapPost("/insert-draft", Insert).WithName("FormNodeInsertDraft")
            .Produces<IReadOnlyList<FormNodeResponse>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409)
            .WithDescription("Atomically inserts a validated draft at target parent/position. Rechecks Create in target, draft version, tree rules and sibling keys; shifts following siblings with Update authorization. Fresh UUIDs, target-derived RBAC/audit, internal configuration references remapped. Returns inserted nodes in parent-first order.");
        group.MapPost("/draft/search", SearchDraft).WithName("FormNodeDraftSearch")
            .Produces<object>().ProducesProblem(400).ProducesProblem(404)
            .WithDescription("Runs configured GraphQL search against a validated detached draft after target Read authorization. Same fixed choices/id/label contract and form-derived variables as normal search; preview values are never saved.");
    }
}
