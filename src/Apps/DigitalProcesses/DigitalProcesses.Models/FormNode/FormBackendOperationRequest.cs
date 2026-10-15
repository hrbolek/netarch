using System.Text.Json.Nodes;
namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
/// <summary>
/// Vstupní kontrakt Form Backend Operation Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record FormBackendOperationRequest(
    string query,
    IReadOnlyCollection<FormBackendParameterBinding> parameters,
    JsonObject? Variables = null);
