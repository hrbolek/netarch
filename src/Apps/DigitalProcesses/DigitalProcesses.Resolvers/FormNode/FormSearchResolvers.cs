using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormNode;
public static class FormSearchResolvers
{
    public static async Task<IResult> Search(FormSearchRequest request, IFormSearchService searches, CancellationToken ct)
        => (await searches.SearchAsync(request, ct)).ToHttpResult();
}
