using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Generic post-query envelope useful when the caller does not need a custom DTO shape.
/// </summary>
public sealed record WithOperations<T>(
    T Value,
    IReadOnlySet<string> Operations);

/// <summary>
/// Post-enrichment helpers. The input query/sequence is materialized first, then all distinct
/// RBAC ids are resolved in one bulk security query. No per-row security query is issued.
/// </summary>
public static class OperationsEnrichmentExtensions
{
    public static async Task<IReadOnlyList<TResult>> EnrichWithOperationsAsync<TInput, TResult>(
        this IEnumerable<TInput> source,
        ISecurityLayer security,
        Func<TInput, Guid> rbacIdSelector,
        Func<TInput, IReadOnlySet<string>, TResult> mapper,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(security);
        ArgumentNullException.ThrowIfNull(rbacIdSelector);
        ArgumentNullException.ThrowIfNull(mapper);

        var items = source as IReadOnlyList<TInput> ?? source.ToArray();
        if (items.Count == 0)
            return Array.Empty<TResult>();

        var rbacIds = items.Select(rbacIdSelector).Distinct().ToArray();
        var operationsByRbac = await security.GetAllowedOperationsAsync(rbacIds, cancellationToken);

        var result = new TResult[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var rbacId = rbacIdSelector(item);
            var operations = operationsByRbac.TryGetValue(rbacId, out var allowed)
                ? allowed
                : EmptyOperations.Instance;
            result[i] = mapper(item, operations);
        }

        return result;
    }

    public static Task<IReadOnlyList<WithOperations<T>>> WithOperationsAsync<T>(
        this IEnumerable<T> source,
        ISecurityLayer security,
        CancellationToken cancellationToken = default)
        where T : ISecuredEntityDTOModel
        => source.EnrichWithOperationsAsync(
            security,
            item => item.rbacobject_id,
            (item, operations) => new WithOperations<T>(item, operations),
            cancellationToken);

    /// <summary>
    /// Convenience overload for deferred queries. Query execution occurs exactly once before enrichment.
    /// Transport-specific filtering/paging should therefore be applied before this method.
    /// </summary>
    public static Task<IReadOnlyList<TResult>> ToListWithOperationsAsync<TInput, TResult>(
        this IQueryable<TInput> query,
        ISecurityLayer security,
        Func<TInput, Guid> rbacIdSelector,
        Func<TInput, IReadOnlySet<string>, TResult> mapper,
        CancellationToken cancellationToken = default)
        => query.ToArray().EnrichWithOperationsAsync(
            security,
            rbacIdSelector,
            mapper,
            cancellationToken);

    private sealed class EmptyOperations : HashSet<string>
    {
        public static readonly IReadOnlySet<string> Instance = new EmptyOperations();
        private EmptyOperations() : base(StringComparer.OrdinalIgnoreCase) { }
    }
}
