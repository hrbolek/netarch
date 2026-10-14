using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DigitalProcesses.Resolvers;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>Approval-required AI form design routes.</summary>
public static class FormDesignAiEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/ai/form-design/approve", FormDesignAiResolvers.Approve)
            .WithTags("DigitalProcesses.AI")
            .WithDescription("Apply an explicitly approved AI form proposal to an editable form version; validates components, keys and RBAC.");
        endpoints.MapPost("/ai/form-design", FormDesignAiResolvers.Design)
            .WithTags("DigitalProcesses.AI")
            .WithDescription("Propose a form schema via configured FastAPI/PydanticAI agent. Checks Read on form version, sends only readable nodes, returns an unpersisted proposal. Never stores agent output.");
    }
}
