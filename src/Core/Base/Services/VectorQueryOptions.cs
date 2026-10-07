namespace WebAppArchitecture.Base.Services;

public sealed record VectorOrderBy(string PropertyName, bool Descending = false);

public sealed record VectorQueryOptions(
    int? Take = null,
    int? Skip = null,
    IReadOnlyCollection<VectorOrderBy>? OrderBy = null)
{
    public static VectorQueryOptions Empty { get; } = new();
}

public sealed record VectorPageInfo(
    int Take,
    int Skip,
    bool HasMore,
    bool HasFilter = false,
    bool HasOrderBy = false)
{
    public bool HasPrevious => Skip > 0;
}

public sealed record VectorPageResult<T>(
    IReadOnlyCollection<T> Items,
    VectorPageInfo PageInfo);
