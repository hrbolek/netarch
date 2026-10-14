using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DigitalProcesses.Resolvers;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>Routes for authorized JSON schema and submission documents.</summary>
public static class FormDocumentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/FormVersionResponse/schema/{id:guid}", FormDocumentResolvers.Schema)
            .WithName("FormVersionJsonSchema").WithTags("DigitalProcesses.FormDocuments")
            .Produces<JsonObject>(200, "application/json")
            .WithDescription("Builds a JSON Schema representation of a form version from its saved FormNode definitions. Repeated sections produce arrays.");
        endpoints.MapGet("/SubmissionResponse/data/{id:guid}", FormDocumentResolvers.Data)
            .WithName("SubmissionJsonData").WithTags("DigitalProcesses.FormDocuments")
            .Produces<JsonObject>(200, "application/json")
            .WithDescription("Reconstructs a submission's JSON document from the saved SubmissionNode tree, including repeated sections and nested keys.");
        endpoints.MapGet("/FormResponse/data/{id:guid}", FormDocumentResolvers.DataList)
            .WithName("FormJsonSubmissionDataList").WithTags("DigitalProcesses.FormDocuments")
            .Produces<JsonArray>(200, "application/json")
            .WithDescription("Returns reconstructed JSON data of readable submissions belonging to the given form definition across all its readable versions. The id is FormDefinitionModel.id, not FormVersionModel.id.");
    }
}
