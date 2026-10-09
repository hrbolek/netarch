namespace WebAppArchitecture.Base.Services;

public sealed record VectorPageResult<T>(
    IReadOnlyCollection<T> Items,
    VectorPageInfo PageInfo);
