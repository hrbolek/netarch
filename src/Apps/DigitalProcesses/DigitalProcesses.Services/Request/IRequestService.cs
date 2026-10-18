using WebAppArchitecture.DigitalProcesses.Models.Request;
using System.Text.Json;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.Request;
/// <summary>
/// Vstupní kontrakt I Request Service. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public interface IRequestService : ISecuredEntityService<RequestModel>
{
    Task<RequestResponse> StartAsync(StartRequestRequest request, CancellationToken ct = default);

    /// <summary>Creates a demo request (with transition history) on the latest version, published or not.</summary>
    Task<RequestResponse> CreateTestRequestAsync(Guid processDefinitionId, CancellationToken ct = default);
    /// <summary>Creates a demo request using a process version that references this exact workflow version.</summary>
    Task<RequestResponse> CreateTestRequestForWorkflowVersionAsync(Guid workflowVersionId, CancellationToken ct = default);
    /// <summary>Creates a demo request using this exact process version, whether published or still a draft.</summary>
    Task<RequestResponse> CreateTestRequestForProcessVersionAsync(Guid processVersionId, CancellationToken ct = default);
    Task<RequestResponse> TransitionAsync(Guid requestId, TransitionRequestRequest request, CancellationToken ct = default);
    Task<RequestDetailResponse> GetDetailAsync(Guid requestId, CancellationToken ct = default);
    Task<RequestResponse?> GetViewAsync(Guid requestId, CancellationToken ct = default);
}
