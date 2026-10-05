using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface ISubmissionService : IBaseService<SubmissionModel, SubmissionResponse> { Task<SubmissionTreeResponse> GetTreeAsync(Guid submissionId,CancellationToken ct=default); }
public sealed class SubmissionService(ISecurityLayer security) : BaseService<SubmissionModel,SubmissionResponse>(security),ISubmissionService
{
    public async Task<SubmissionTreeResponse> GetTreeAsync(Guid submissionId,CancellationToken ct=default){var s=await Security.GetByIdAsync<SubmissionModel>(submissionId,SecurityOperations.Read,ct)??throw new KeyNotFoundException();var nodes=Security.Query<SubmissionNodeModel>(SecurityOperations.Read).Where(x=>x.submission_id==submissionId).OrderBy(x=>x.path).ThenBy(x=>x.position).Select(SubmissionNodeResponse.Projection).ToArray();return new(SubmissionResponse.FromModel(s),nodes);}
}
