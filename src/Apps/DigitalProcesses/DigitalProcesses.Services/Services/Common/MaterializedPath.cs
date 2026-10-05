namespace WebAppArchitecture.DigitalProcesses.Services;

public static class MaterializedPath
{
    public static string Root(Guid id) => "/" + Segment(id);
    public static string Child(string parentPath, Guid id) => parentPath.TrimEnd('/') + "/" + Segment(id);
    public static bool IsDescendantOrSelf(string path, string ancestorPath)
        => path == ancestorPath || path.StartsWith(ancestorPath.TrimEnd('/') + "/", StringComparison.Ordinal);
    private static string Segment(Guid id) => id.ToString("N").ToLowerInvariant();
}
