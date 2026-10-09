using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Swashbuckle.AspNetCore.Swagger;

namespace WebAppArchitecture.Hosting;

/// <summary>Exports the same generated OpenAPI document that backs Swagger UI.</summary>
public static class SwaggerExportExtensions
{
    public static async Task ExportSwaggerJsonAsync(
        this WebApplication app,
        string path = "openapi/swagger.json",
        string documentName = "v1",
        CancellationToken cancellationToken = default)
    {
        var provider = app.Services.GetRequiredService<ISwaggerProvider>();
        var document = provider.GetSwagger(documentName);
        if (document.Paths is null || document.Paths.Count == 0)
        {
            throw new InvalidOperationException(
                $"OpenAPI document '{documentName}' contains no paths. " +
                "Ensure the application has started and its endpoints are registered before exporting.");
        }

        var json = document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        var absolutePath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(app.Environment.ContentRootPath, path);
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(absolutePath, json, cancellationToken);
        app.Logger.LogInformation("OpenAPI document {DocumentName} exported to {Path}", documentName, absolutePath);
    }
}
