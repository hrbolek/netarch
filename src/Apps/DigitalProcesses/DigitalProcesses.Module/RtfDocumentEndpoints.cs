using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DigitalProcesses.Resolvers;

namespace WebAppArchitecture.DigitalProcesses;

public static class RtfDocumentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
        => RtfDocumentResolvers.Map(endpoints);
}
