namespace WebAppArchitecture.Base.Services;

public sealed record VectorQueryOptions(
    int? Take = null,
    int? Skip = null,
    IReadOnlyCollection<VectorOrderBy>? OrderBy = null)
{
    public static VectorQueryOptions Empty { get; } = new();
}
