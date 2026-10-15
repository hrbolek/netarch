using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>
/// Typ I Form Expression Evaluator ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public interface IFormExpressionEvaluator
{
    JsonNode? Evaluate(JsonObject context, string expression);
}
