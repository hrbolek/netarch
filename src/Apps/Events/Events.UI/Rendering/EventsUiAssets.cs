using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.Events;

public static class EventsUiAssets
{
    public static IResult Get(string file)
    {
        if (file is not ("academic-planner.css" or "academic-planner.js"))
            return Results.NotFound();

        var resourceName = $"WebAppArchitecture.Events.events-assets.{file}";
        using var stream = typeof(EventsUiAssets).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return Results.NotFound();

        using var reader = new StreamReader(stream);
        var contentType = file.EndsWith(".css", StringComparison.Ordinal)
            ? "text/css; charset=utf-8"
            : "text/javascript; charset=utf-8";
        return Results.Content(reader.ReadToEnd(), contentType);
    }
}
