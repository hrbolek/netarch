using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed record FormBackendPreparedRequest(string query, JsonObject variables);
