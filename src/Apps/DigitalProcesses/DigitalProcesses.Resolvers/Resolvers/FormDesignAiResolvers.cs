using Microsoft.AspNetCore.Http;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class FormDesignAiResolvers
{
    public static async Task<IResult> Design(FormDesignRequest request, IFormDesignAiService designer, CancellationToken ct)
        => (await designer.DesignAsync(request, ct)).ToHttpResult();

    public static async Task<IResult> Approve(ApproveFormDesignRequest request, IFormDesignAiService designer, CancellationToken ct)
        => (await designer.ApproveAsync(request, ct)).ToHttpResult();
}
