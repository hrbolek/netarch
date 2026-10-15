using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;
/// <summary>
/// Typ Form Data Context Builder ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed class FormDataContextBuilder : IFormDataContextBuilder
{
    public JsonObject Build(IReadOnlyCollection<FormNodeResponse> nodes,
        IReadOnlyDictionary<string, JsonNode?> visibleValues,
        FormContextMetadata? metadata = null,
        IEnumerable<FormContextHistoryEntry>? visibleHistory = null)
    {
        metadata ??= new FormContextMetadata();
        var form = new JsonObject();
        var lookup = nodes.OrderBy(n => n.position).ThenBy(n => n.key)
            .ToLookup(x => x.parent_id);
        var traversed = new HashSet<Guid>();
        Fill(form, null, lookup, visibleValues, traversed);
        var history = new JsonArray();
        foreach (var item in (visibleHistory ?? []).OrderBy(x => x.ChangedAt).ThenBy(x => x.ArchivedSubmissionId))
        {
            history.Add(new JsonObject
            {
                ["state"] = new JsonObject { ["id"] = item.StateId.ToString(), ["name"] = item.StateName },
                ["who"] = new JsonObject { ["id"] = item.ActorUserId.ToString(), ["email"] = item.ActorEmail },
                ["note"] = item.PublicNote,
                ["when"] = item.ChangedAt.ToUniversalTime().ToString("O"),
                ["submission_id"] = item.ArchivedSubmissionId.ToString()
            });
        }
        return new JsonObject
        {
            ["form"] = form,
            ["request"] = new JsonObject { ["id"] = metadata.RequestId?.ToString() },
            ["submission"] = new JsonObject { ["id"] = metadata.SubmissionId?.ToString() },
            ["workflow"] = new JsonObject
            {
                ["id"] = metadata.WorkflowVersionId?.ToString(),
                ["state"] = new JsonObject
                {
                    ["id"] = metadata.CurrentStateId?.ToString(),
                    ["name"] = metadata.CurrentStateName
                }
            },
            ["history"] = history
        };
    }

    private static void Fill(JsonObject output, Guid? parentId,
        ILookup<Guid?, FormNodeResponse> nodes,
        IReadOnlyDictionary<string, JsonNode?> visibleValues, HashSet<Guid> traversed)
    {
        foreach (var child in nodes[parentId])
        {
            if (!traversed.Add(child.id)) throw new InvalidOperationException("Cycle in FormNodes.");
            if (!Regex.IsMatch(child.key, "^[A-Za-z_][A-Za-z0-9_]*$"))
                throw new InvalidOperationException($"Invalid FormNode key '{child.key}'.");
            if (output.ContainsKey(child.key)) throw new InvalidOperationException($"Duplicate FormNode key '{child.key}'.");
            if (nodes.Contains(child.id))
            {
                var section = new JsonObject();
                Fill(section, child.id, nodes, visibleValues, traversed);
                output[child.key] = section;
            }
            else if (visibleValues.TryGetValue(child.id.ToString(), out var value)
                  || visibleValues.TryGetValue(child.id.ToString("N"), out value)
                  || visibleValues.TryGetValue(child.path, out value)
                  || visibleValues.TryGetValue(child.key, out value))
            {
                output[child.key] = value?.DeepClone();
            }
            // Missing/denied values are deliberately absent, not null.
        }
    }
}
