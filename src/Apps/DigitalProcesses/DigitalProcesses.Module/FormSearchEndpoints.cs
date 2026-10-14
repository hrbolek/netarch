using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DigitalProcesses.Resolvers;

namespace WebAppArchitecture.DigitalProcesses;

public static class FormSearchEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/form-node-search", FormSearchResolvers.Search)
            .WithTags("DigitalProcesses.Forms")
            .WithDescription("Searches a configured GraphQL-backed form lookup. The query comes from an RBAC-readable form node, never from the caller.");
    }
}
