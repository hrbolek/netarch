using Microsoft.AspNetCore.Http;
using WebAppArchitecture.DigitalProcesses.Services;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class FormComponentCatalogResolvers
{
    public static IResult GetCatalog(IFormComponentCatalogService catalog)
        => catalog.GetCatalog().ToHttpResult();
}
