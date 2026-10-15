using System.Text.Json;
namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;
// Omitted nodes are preserved. No client-supplied RBAC IDs or audit metadata.
/// <summary>
/// Vstupní kontrakt Bulk Form Tree Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record BulkFormTreeRequest(
    IReadOnlyList<BulkFormTreeNode>? nodes = null,
    JsonElement? children = null);
