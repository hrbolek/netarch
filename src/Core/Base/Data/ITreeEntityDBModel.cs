namespace WebAppArchitecture.Base.Data;

/// <summary>
/// Secured self-referencing tree node. All ancestors are identified by the materialized <see cref="IEntityWithTree.path"/>,
/// so subtree, ancestor and child queries never depend on recursive SQL.
/// </summary>
/// <remarks>
/// The path has the form <c>/{rootId}/.../{id}/</c> (see <see cref="TreePath"/>). <see cref="WithPath"/> returns the node
/// carrying the new path; immutable records return a copy, mutable classes may return themselves.
/// </remarks>

public interface ITreeEntityDBModel<TSelf> : ISecuredEntityDBModel, IEntityWithTree
    where TSelf : ITreeEntityDBModel<TSelf>
{
    Guid? parent_id { get; }
    TSelf WithPath(string path);
}

/// <summary>Encoding of the materialized path shared by all tree entities.</summary>
public static class TreePath
{
    /// <summary>Length of one path segment: a GUID in "D" format plus the trailing slash.</summary>
    public const int SegmentLength = 37;

    public static string Root(Guid id) => $"/{id:D}/";
    public static string Child(string parentPath, Guid id) => $"{parentPath}{id:D}/";
    public static bool ContainsNode(string path, Guid id) => path.Contains($"/{id:D}/", StringComparison.OrdinalIgnoreCase);

    /// <summary>Ids of all ancestors in root-to-parent order, excluding the node itself.</summary>
    public static IReadOnlyList<Guid> AncestorIds(string path)
    {
        var ids = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty).Where(x => x != Guid.Empty).ToList();
        if (ids.Count > 0) ids.RemoveAt(ids.Count - 1);
        return ids;
    }
}
