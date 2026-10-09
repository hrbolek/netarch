using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IProcessDefinitionVersionService : IBaseService<ProcessDefinitionVersionModel, ProcessDefinitionVersionResponse> { Task<ProcessDefinitionVersionResponse> CreateContainedAsync(CreateProcessDefinitionVersionRequest request,CancellationToken ct=default); Task<ProcessDefinitionVersionResponse> PublishAsync(Guid id,CancellationToken ct=default); Task DeleteDraftAsync(Guid id,CancellationToken ct=default); Task<ProcessDefinitionVersionResponse> CloneLatestAsync(Guid definitionId,CancellationToken ct=default); }
