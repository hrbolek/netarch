namespace WebAppArchitecture.DigitalProcesses.Services.Shared;
/// <summary>
/// Typ Materialized Path ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public static class MaterializedPath
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static string Root(Guid id) => "/" + Segment(id);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static string Child(string parentPath, Guid id) => parentPath.TrimEnd('/') + "/" + Segment(id);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static bool IsDescendantOrSelf(string path, string ancestorPath)
        => path == ancestorPath || path.StartsWith(ancestorPath.TrimEnd('/') + "/", StringComparison.Ordinal);
    private static string Segment(Guid id) => id.ToString("N").ToLowerInvariant();
}
