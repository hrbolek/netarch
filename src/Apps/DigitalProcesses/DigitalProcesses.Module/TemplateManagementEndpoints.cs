using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DigitalProcesses.Resolvers;

namespace WebAppArchitecture.DigitalProcesses;

public static class TemplateManagementEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
        => TemplateManagementResolvers.Map(endpoints);
}
