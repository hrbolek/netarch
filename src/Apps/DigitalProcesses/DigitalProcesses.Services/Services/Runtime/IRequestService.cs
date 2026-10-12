using System.Text.Json;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Vstupní kontrakt I Request Service. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public interface IRequestService : IBaseService<RequestModel, RequestResponse>
{
    Task<RequestResponse> StartAsync(StartRequestRequest request, CancellationToken ct = default);
    Task<RequestResponse> TransitionAsync(Guid requestId, TransitionRequestRequest request, CancellationToken ct = default);
    Task<RequestDetailResponse> GetDetailAsync(Guid requestId, CancellationToken ct = default);
}
