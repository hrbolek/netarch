using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
public sealed record FormSearchValue(string Name, JsonElement Value);
