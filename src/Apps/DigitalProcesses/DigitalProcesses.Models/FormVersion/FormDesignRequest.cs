using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;
public sealed record FormDesignRequest(Guid FormVersionId, string Instruction);
