using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.AuthorizationLayer;

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
