using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;

public sealed record FormSearchRequest(
    Guid NodeId, string Pattern, IReadOnlyList<FormSearchValue>? Values = null);
