using System.Text.Json;
namespace WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
/// <summary>
/// Vstupní kontrakt Create Form Definition Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record CreateFormDefinitionRequest(
    string name,
    string? description = null,
    JsonElement? children = null);
