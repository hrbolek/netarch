using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Text.Json;
namespace WebAppArchitecture.DigitalProcesses.Models;

// Omitted nodes are preserved. No client-supplied RBAC IDs or audit metadata.
/// <summary>
/// Vstupní kontrakt Bulk Form Tree Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record BulkFormTreeRequest(
    IReadOnlyList<BulkFormTreeNode>? nodes = null,
    JsonElement? children = null);
/// <summary>
/// Typ Bulk Form Tree Node ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed record BulkFormTreeNode(
    Guid? id, string key, Guid node_type_id,
    string? label = null, string? description = null,
    int position = 0, int min_occurs = 1, int max_occurs = 1,
    string? configuration_json = null,
    IReadOnlyList<BulkFormTreeNode>? children = null);
/// <summary>
/// Vstupní kontrakt Bulk Submission Values Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record BulkSubmissionValuesRequest(IReadOnlyList<BulkSubmissionValue> nodes);
/// <summary>
/// Typ Bulk Submission Value ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed record BulkSubmissionValue(Guid id, string? value_json);
