using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.Submission;
public static class FormDocumentResolvers
{
    public static async Task<IResult> Schema(Guid id, IFormDocumentService documents, CancellationToken ct)
        => (await documents.SchemaAsync(id, ct)).ToHttpResult();

    public static async Task<IResult> Data(Guid id, IFormDocumentService documents, CancellationToken ct)
        => (await documents.DataAsync(id, ct)).ToHttpResult();

    public static async Task<IResult> DataList(Guid id, IFormDocumentService documents, CancellationToken ct)
        => (await documents.DataListAsync(id, ct)).ToHttpResult();
}
