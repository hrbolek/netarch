using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class TemplateManagementResolvers
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        const string prefix = "/document-templates";
        endpoints.MapGet(prefix + "/{id:guid}/revisions", Revisions)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("List immutable revisions of a template; requires Read on its FormVersion.");
        endpoints.MapGet(prefix + "/revisions/{revisionId:guid}/content", DownloadRevision)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Download an exact immutable RTF revision.");
        endpoints.MapPost(prefix + "/{id:guid}/revisions", CreateRevision)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Create a new RTF revision with verified FormNode bindings; requires Update on FormVersion.");
        endpoints.MapGet(prefix + "/form-schema/{formVersionId:guid}", FormSchema)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Schema context for future AI template design. Returns only readable FormNodes.");
    }

    public static async Task<IResult> Revisions(Guid id, IRtfTemplateService service, CancellationToken ct)
    {
        var revisions = await service.ListRevisionsAsync(id, ct);
        return revisions is null ? Results.NotFound() : Results.Json(revisions);
    }

    public static async Task<IResult> DownloadRevision(Guid revisionId, IRtfTemplateService service, CancellationToken ct)
    {
        var revision = await service.DownloadRevisionAsync(revisionId, ct);
        return revision is null
            ? Results.NotFound()
            : Results.File(revision.Content, revision.ContentType, $"template-{revision.TemplateId:N}-r{revision.Revision}.rtf");
    }

    public static async Task<IResult> FormSchema(Guid formVersionId, IRtfTemplateService service, CancellationToken ct)
    {
        var nodes = await service.GetFormSchemaAsync(formVersionId, ct);
        return nodes is null ? Results.NotFound() : Results.Json(nodes);
    }

    public static async Task<IResult> CreateRevision(
        Guid id, NewRtfRevisionRequest request, IRtfTemplateService service, CancellationToken ct)
    {
        var result = await service.CreateRevisionAsync(id, request, ct);
        if (!result.Found) return Results.NotFound();
        if (result.Error is not null) return Results.BadRequest(new { error = result.Error });
        var revision = result.Revision!;
        return Results.Created(
            $"/digital-processes/document-templates/revisions/{revision.id}/content",
            new { revision.id, revision.revision });
    }
}
