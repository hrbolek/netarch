namespace WebAppArchitecture.Base.Services;

public sealed record VectorPageInfo(
    int Take,
    int Skip,
    bool HasMore,
    bool HasFilter = false,
    bool HasOrderBy = false)
{
    public bool HasPrevious => Skip > 0;
}
