using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.DocumentTemplate;

public static class RtfDocumentResolvers
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/FormVersionResponse/rtf-templates/{id:guid}", List)
            .WithTags("DigitalProcesses.Rtf").WithName("FormRtfTemplateList")
            .WithDescription("Lists stored RTF templates associated with this form version, subject to RBAC Read.");
        endpoints.MapPost("/FormVersionResponse/rtf-templates/{id:guid}", Upload)
            .WithTags("DigitalProcesses.Rtf").WithName("FormRtfTemplateUpload")
            .Accepts<byte[]>("application/rtf")
            .WithDescription("Stores an RTF template. Send raw application/rtf bytes; optional ?name=... . Requires Update on the form version.");
        endpoints.MapPost("/FormVersionResponse/rtf-template/generate/{id:guid}", Generate)
            .WithTags("DigitalProcesses.Rtf").WithName("FormRtfTemplateGenerate")
            .WithDescription("Generates and persists a starter RTF template from saved FormNodes, including nested objects and repeated sections. Requires Update.");
        endpoints.MapGet("/RtfTemplate/{id:guid}", Download)
            .WithTags("DigitalProcesses.Rtf").WithName("RtfTemplateDownload")
            .WithDescription("Downloads the source RTF template, subject to form-version Read.");
        endpoints.MapDelete("/RtfTemplate/{id:guid}", Delete)
            .WithTags("DigitalProcesses.Rtf").WithName("RtfTemplateDelete")
            .WithDescription("Deletes a stored template. Requires form-version Update.");
        endpoints.MapGet("/RtfTemplate/{id:guid}/submission/{submissionId:guid}", Render)
            .WithTags("DigitalProcesses.Rtf").WithName("SubmissionRenderRtf")
            .WithDescription("Downloads an RTF document rendered using this template and a readable Submission of the same form version. Reads values from SubmissionNodes.");
    }

    public static async Task<IResult> List(Guid id, IRtfTemplateService service, CancellationToken ct)
    {
        var items = await service.ListAsync(id, ct);
        return items is null ? Results.NotFound() : Results.Json(items);
    }

    public static async Task<IResult> Upload(Guid id, HttpRequest request, IRtfTemplateService service, CancellationToken ct)
    {
        var result = await service.UploadAsync(id, request.ContentType, request.Body, request.Query["name"].ToString(), ct);
        if (!result.Found) return Results.NotFound();
        if (result.ErrorStatus == 415) return Results.Problem(result.Error, statusCode: 415);
        if (result.Error is not null) return Results.BadRequest(new { error = result.Error });
        return Results.Created($"/digital-processes/RtfTemplate/{result.Template!.Id}", result.Template);
    }

    public static async Task<IResult> Generate(Guid id, IRtfTemplateService service, CancellationToken ct)
    {
        var result = await service.GenerateAsync(id, ct);
        if (!result.Found) return Results.NotFound();
        return Results.Created($"/digital-processes/RtfTemplate/{result.Template!.Id}", result.Template);
    }

    public static async Task<IResult> Download(Guid id, IRtfTemplateService service, CancellationToken ct)
    {
        var result = await service.DownloadAsync(id, ct);
        return result is null ? Results.NotFound() : Results.File(result.Content, "application/rtf", "template.rtf");
    }

    public static async Task<IResult> Delete(Guid id, IRtfTemplateService service, CancellationToken ct)
        => await service.DeleteTemplateAsync(id, ct) ? Results.NoContent() : Results.NotFound();

    public static async Task<IResult> Render(Guid id, Guid submissionId, IRtfTemplateService service, CancellationToken ct)
    {
        var result = await service.RenderAsync(id, submissionId, ct);
        if (!result.Found) return Results.NotFound();
        if (result.Error is not null) return Results.BadRequest(new { error = result.Error });
        return Results.File(result.Content!, "application/rtf", $"submission-{submissionId:N}.rtf");
    }
}
