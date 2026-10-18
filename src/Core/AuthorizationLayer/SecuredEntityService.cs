using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>
/// Secure-by-default entity service independent of a particular DTO representation.
/// Only whitelisted DTO contracts can be projected from SQL. No raw database query
/// is exposed to HTTP resolvers or API clients.
/// </summary>

public abstract class SecuredEntityService<TDBModel> : ISecuredEntityService<TDBModel>
    where TDBModel : class, ISecuredEntityDBModel
{
    protected readonly IAuthorizationLayer Security;
    private readonly DataOperations<TDBModel> _data;
    private readonly ISecurityQueryProvider _provider;

    protected SecuredEntityService(IAuthorizationLayer security, IDBLayer db, ISecurityQueryProvider? provider = null)
    {
        Security = security;
        _data = new DataOperations<TDBModel>(db);
        _provider = provider ?? new RbacSecurityQueryProvider(security);
    }

    /// <summary>Privileged, unfiltered database source; for internal invariants only.</summary>
    protected IQueryable<TDBModel> Unsecured_Query() => _data.Query();
    protected Task<TDBModel?> Unsecured_ReadAsync(Guid id, CancellationToken ct = default) => _data.ReadAsync(id, ct);
    protected Task<TDBModel> Unsecured_StageCreateAsync(TDBModel model, CancellationToken ct = default) => _data.StageCreateAsync(model, ct);
    protected Task<TDBModel> Unsecured_StageUpdateAsync(TDBModel model, CancellationToken ct = default) => _data.StageUpdateAsync(model, ct);
    protected Task Unsecured_StageDeleteAsync(Guid id, CancellationToken ct = default) => _data.StageDeleteAsync(id, ct);

    /// <summary>
    /// Secured entity source for internally composing SQL queries. Never expose the
    /// underlying model through a public service contract: field-level sensitivity
    /// is enforced by approved DTO projections, not by row-level RBAC alone.
    /// </summary>
    protected IQueryable<TDBModel> SecuredModels(string operation = SecurityOperations.List)
        => _provider.Query<TDBModel>(operation);

    /// <summary>
    /// Compose an independently RBAC-filtered related source in the same EF context.
    /// A relationship navigation in the DTO projection alone does NOT enforce RBAC
    /// on the related entity. A separate policy must be applied when required.
    /// </summary>
    protected IQueryable<TRelated> SecuredRelated<TRelated>(string operation = SecurityOperations.Read)
        where TRelated : class, ISecuredEntityDBModel
        => _provider.Query<TRelated>(operation);

    /// <summary>SQL projection through a trusted typed DTO contract, after RBAC filtering.</summary>
    public IQueryable<TDTO> Query<TDTO>()
        where TDTO : IEntityProjection<TDBModel, TDTO>
        => SecuredModels().Select(TDTO.Projection);

    /// <summary>Read one authorized entity and project to the requested DTO.</summary>
    public Task<TDTO?> GetByIdAsync<TDTO>(Guid id, CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>
        => Task.FromResult(SecuredModels(SecurityOperations.Read)
            .Where(entity => entity.id == id)
            .Select(TDTO.Projection)
            .SingleOrDefault());

    /// <summary>List authorized entities as the requested DTO projection.</summary>
    public Task<IReadOnlyCollection<TDTO>> GetAllAsync<TDTO>(CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>
        => Task.FromResult<IReadOnlyCollection<TDTO>>(Query<TDTO>().ToArray());

    /// <summary>
    /// Build a predicate-filtered and authorized DTO projection. The predicate is
    /// applied inside SQL, not to materialized objects.
    /// </summary>
    public IQueryable<TDTO> Where<TDTO>(Expression<Func<TDBModel, bool>> predicate, string operation = SecurityOperations.List)
        where TDTO : IEntityProjection<TDBModel, TDTO>
        => SecuredModels(operation).Where(predicate).Select(TDTO.Projection);

    /// <summary>
    /// Update an authorized entity in the existing security transaction. The update
    /// request controls writable fields; the original RBAC context is checked before
    /// applying changes. The result DTO is chosen by the caller.
    /// </summary>
    public virtual async Task<TDTO> UpdateAsync<TUpdateRequest, TDTO>(TUpdateRequest model, CancellationToken cancellationToken = default)
        where TUpdateRequest : IUpdateDTOModel<TDBModel>
        where TDTO : IEntityMapper<TDBModel, TDTO>
    {
        var current = await Security.GetByIdAsync<TDBModel>(model.id, SecurityOperations.Update, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id '{model.id}' does not exist or is not accessible.");
        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var updated = await Security.StageUpdateAsync(model.ApplyTo(current), model.updated_at, ct);
            await Security.SaveChangesAsync(ct);
            return TDTO.FromModel(updated);
        }, cancellationToken);
    }

    public virtual Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(
        Expression<Func<TDBModel, bool>> predicate,
        CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>
        => VectorAsync<TDTO>(predicate, SecurityOperations.List, cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>
        => VectorAsync<TDTO>(predicate, operation, VectorQueryOptions.Empty, cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        int? take,
        CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>
        => VectorAsync<TDTO>(predicate, operation, new VectorQueryOptions(Take: take), cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>
    {
        var query = BuildVectorQuery(predicate, operation, options, probeForMore: false);
        return Task.FromResult<IReadOnlyCollection<TDTO>>(
            query.Select(TDTO.Projection).ToArray());
    }

    public virtual Task<VectorPageResult<TDTO>> VectorPageAsync<TDTO>(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>
    {
        var take = options.Take ?? 25;
        var skip = options.Skip ?? 0;
        var query = BuildVectorQuery(predicate, operation, options with { Take = take }, probeForMore: true);
        var values = query.Select(TDTO.Projection).ToArray();
        var hasMore = values.Length > take;
        IReadOnlyCollection<TDTO> items = hasMore ? values.Take(take).ToArray() : values;
        return Task.FromResult(new VectorPageResult<TDTO>(items, new VectorPageInfo(take, skip, hasMore)));
    }


    protected IQueryable<TDBModel> BuildVectorQuery(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        bool probeForMore)
    {
        IQueryable<TDBModel> query = SecuredModels(operation)
            .Where(predicate);

        query = ApplyOrdering(query, options.OrderBy);

        if (options.Skip is > 0)
            query = query.Skip(options.Skip.Value);

        if (options.Take is > 0)
            query = query.Take(options.Take.Value + (probeForMore ? 1 : 0));

        return query;
    }

    protected static IQueryable<TModel> ApplyOrdering<TModel>(
        IQueryable<TModel> query,
        IReadOnlyCollection<VectorOrderBy>? orderBy)
    {
        var requested = orderBy?.ToList() ?? new List<VectorOrderBy>();
        if (requested.Count == 0)
            requested.Add(new VectorOrderBy("id"));
        else if (!requested.Any(item => string.Equals(item.PropertyName, "id", StringComparison.OrdinalIgnoreCase)))
            requested.Add(new VectorOrderBy("id"));

        var first = true;
        foreach (var item in requested)
        {
            var property = typeof(TModel).GetProperty(
                item.PropertyName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.IgnoreCase)
                ?? throw new InvalidOperationException($"Unknown order property '{item.PropertyName}'.");

            var parameter = Expression.Parameter(typeof(TModel), "entity");
            var body = Expression.Property(parameter, property);
            var keySelector = Expression.Lambda(body, parameter);

            var methodName = first
                ? (item.Descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy))
                : (item.Descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy));

            var method = typeof(Queryable).GetMethods()
                .Single(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TModel), property.PropertyType);

            query = (IQueryable<TModel>)method.Invoke(null, new object[] { query, keySelector })!;
            first = false;
        }

        return query;
    }

    /// <summary>Delete after the security layer's operation-specific authorization.</summary>
    public virtual Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<TDBModel>(id, ct);
            await Security.SaveChangesAsync(ct);
        }, cancellationToken);
}
