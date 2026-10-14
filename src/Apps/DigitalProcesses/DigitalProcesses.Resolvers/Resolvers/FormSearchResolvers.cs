using Microsoft.AspNetCore.Http;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class FormSearchResolvers
{
    public static async Task<IResult> Search(FormSearchRequest request, IFormSearchService searches, CancellationToken ct)
        => (await searches.SearchAsync(request, ct)).ToHttpResult();
}
