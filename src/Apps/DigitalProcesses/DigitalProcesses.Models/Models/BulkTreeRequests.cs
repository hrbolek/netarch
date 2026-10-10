namespace WebAppArchitecture.DigitalProcesses.Models;

// Omitted nodes are preserved. No client-supplied RBAC IDs or audit metadata.
public sealed record BulkFormTreeRequest(IReadOnlyList<BulkFormTreeNode> nodes);
public sealed record BulkFormTreeNode(
    Guid? id, string key, Guid node_type_id,
    string? label = null, string? description = null,
    int position = 0, int min_occurs = 1, int max_occurs = 1,
    string? configuration_json = null,
    IReadOnlyList<BulkFormTreeNode>? children = null);
public sealed record BulkSubmissionValuesRequest(IReadOnlyList<BulkSubmissionValue> nodes);
public sealed record BulkSubmissionValue(Guid id, string? value_json);
