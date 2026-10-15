using WebAppArchitecture.DigitalProcesses.Resolvers.DocumentTemplate;
using Microsoft.AspNetCore.Routing;

namespace WebAppArchitecture.DigitalProcesses;

public static class RtfDocumentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
        => RtfDocumentResolvers.Map(endpoints);
}
