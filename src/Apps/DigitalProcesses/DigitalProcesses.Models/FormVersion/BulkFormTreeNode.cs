using System.Text.Json;
namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;
/// <summary>
/// Typ Bulk Form Tree Node ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed record BulkFormTreeNode(
    Guid? id, string key, Guid node_type_id,
    string? label = null, string? description = null,
    int position = 0, int min_occurs = 1, int max_occurs = 1,
    string? configuration_json = null,
    IReadOnlyList<BulkFormTreeNode>? children = null);
