using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba I Process Definition Version Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IProcessDefinitionVersionService : IBaseService<ProcessDefinitionVersionModel> { Task<ProcessDefinitionVersionResponse> CreateContainedAsync(CreateProcessDefinitionVersionRequest request,CancellationToken ct=default); Task<ProcessDefinitionVersionResponse> PublishAsync(Guid id,CancellationToken ct=default); Task DeleteDraftAsync(Guid id,CancellationToken ct=default); Task<ProcessDefinitionVersionResponse> CloneLatestAsync(Guid definitionId,CancellationToken ct=default); }
