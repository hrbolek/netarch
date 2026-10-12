using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>One header forwarding policy shared by form import and JSONata playground.</summary>
public static class FormBackendHeaderForwarder
{
    public static void Apply(HttpRequest? incoming, HttpRequestMessage outgoing, FormDataBackendOptions options)
    {
        // The JSONata/GraphQL API must receive a JSON representation, regardless of
        // the browser's Accept header (typically text/html or */*).
        // Apply this also when there is no incoming HTTP request.
        outgoing.Headers.Accept.Clear();
        outgoing.Headers.Accept.ParseAdd("application/json");

        if (incoming is null) return;
        foreach (var name in options.ForwardHeaders.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name) ||
                name.Equals("host", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("content-length", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("content-type", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("accept", StringComparison.OrdinalIgnoreCase) ||
                !incoming.Headers.TryGetValue(name, out var values)) continue;
            outgoing.Headers.TryAddWithoutValidation(name, values.ToArray());
        }
    }
}
