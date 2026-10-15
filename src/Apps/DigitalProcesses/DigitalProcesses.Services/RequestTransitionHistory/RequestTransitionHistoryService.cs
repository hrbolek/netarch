using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.RequestTransitionHistory;
/// <summary>
/// Vstupní kontrakt Request Transition History Service. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed class RequestTransitionHistoryService(ISecurityLayer security, IDBLayer unsecuredDb) : SecuredBaseService<RequestTransitionHistoryModel>(security, unsecuredDb),IRequestTransitionHistoryService { }
