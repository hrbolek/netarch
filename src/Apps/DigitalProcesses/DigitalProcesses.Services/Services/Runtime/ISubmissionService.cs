using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface ISubmissionService : IBaseService<SubmissionModel, SubmissionResponse> { Task<SubmissionTreeResponse> GetTreeAsync(Guid submissionId,CancellationToken ct=default); }
