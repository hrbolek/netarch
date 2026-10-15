using WebAppArchitecture.DigitalProcesses.Resolvers.FormNodeType;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>Agent-facing inventory of installed, readable form elements.</summary>
public static class FormComponentCatalogEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/form-component-catalog", FormComponentCatalogResolvers.GetCatalog)
            .WithTags("DigitalProcesses.AI")
            .WithDescription("Returns readable, implemented form node types and complete example configurations for schema-generating agents. RBAC-filtered.");
    }
}
