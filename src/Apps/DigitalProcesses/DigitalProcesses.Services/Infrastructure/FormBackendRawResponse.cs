using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>
/// Výstupní DTO entity Form Backend Raw response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record FormBackendRawResponse(JsonNode? payload);
