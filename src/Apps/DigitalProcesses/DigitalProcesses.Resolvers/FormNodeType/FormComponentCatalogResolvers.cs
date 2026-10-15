using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormNodeType;
public static class FormComponentCatalogResolvers
{
    public static IResult GetCatalog(IFormComponentCatalogService catalog)
        => catalog.GetCatalog().ToHttpResult();
}
