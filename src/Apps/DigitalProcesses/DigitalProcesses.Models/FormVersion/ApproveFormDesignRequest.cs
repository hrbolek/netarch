using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;

public sealed record ApproveFormDesignRequest(Guid FormVersionId, JsonElement Schema);
