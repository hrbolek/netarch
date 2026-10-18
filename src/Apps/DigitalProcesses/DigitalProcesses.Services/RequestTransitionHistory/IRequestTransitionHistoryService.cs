using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.RequestTransitionHistory;
/// <summary>
/// Vstupní kontrakt I Request Transition History Service. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public interface IRequestTransitionHistoryService : ISecuredEntityService<RequestTransitionHistoryModel> { }
