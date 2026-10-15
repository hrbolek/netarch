using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>
/// Typ I Form Url Template Renderer ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public interface IFormUrlTemplateRenderer
{
    FormUrlResult Render(string template, JsonObject context);
}
