using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;
/// <summary>
/// Typ I Form Data Context Builder ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public interface IFormDataContextBuilder
{
    JsonObject Build(IReadOnlyCollection<FormNodeResponse> nodes,
        IReadOnlyDictionary<string, JsonNode?> visibleValues,
        FormContextMetadata? metadata = null,
        IEnumerable<FormContextHistoryEntry>? visibleHistory = null);
}
