using WebAppArchitecture.DigitalProcesses.Resolvers.DocumentTemplate;
using Microsoft.AspNetCore.Routing;

namespace WebAppArchitecture.DigitalProcesses;

public static class TemplateManagementEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
        => TemplateManagementResolvers.Map(endpoints);
}
