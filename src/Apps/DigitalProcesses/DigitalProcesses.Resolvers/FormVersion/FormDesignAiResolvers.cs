using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormVersion;
public static class FormDesignAiResolvers
{
    public static async Task<IResult> Design(FormDesignRequest request, IFormDesignAiService designer, CancellationToken ct)
        => (await designer.DesignAsync(request, ct)).ToHttpResult();

    public static async Task<IResult> Approve(ApproveFormDesignRequest request, IFormDesignAiService designer, CancellationToken ct)
        => (await designer.ApproveAsync(request, ct)).ToHttpResult();
}
