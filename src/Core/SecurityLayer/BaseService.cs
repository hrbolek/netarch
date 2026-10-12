using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Služba Base Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public class BaseService<TDBModel, TDTOModel> : IBaseService<TDBModel, TDTOModel>
    where TDBModel : class, ISecuredEntityDBModel
    where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
{
    protected readonly ISecurityLayer Security;
    public BaseService(ISecurityLayer security) => Security = security;

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: Query; pořadí a parametry viz implementace.
    /// </summary>
    public virtual IQueryable<TDTOModel> Query() => Security.Query<TDBModel>(SecurityOperations.List).Select(TDTOModel.Projection);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Query; pořadí a parametry viz implementace.
    /// </summary>
    public virtual Task<TDTOModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(
            Security.Query<TDBModel>(SecurityOperations.Read)
                .Where(entity => entity.id == id)
                .Select(TDTOModel.Projection)
                .SingleOrDefault());
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Query; pořadí a parametry viz implementace.
    /// </summary>
    public virtual Task<IReadOnlyCollection<TDTOModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<TDTOModel>>(
            Security.Query<TDBModel>(SecurityOperations.List)
                .Select(TDTOModel.Projection)
                .ToArray());
    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        CancellationToken cancellationToken = default)
        => VectorAsync(predicate, SecurityOperations.List, cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        CancellationToken cancellationToken = default)
        => VectorAsync(predicate, operation, VectorQueryOptions.Empty, cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        int? take,
        CancellationToken cancellationToken = default)
        => VectorAsync(predicate, operation, new VectorQueryOptions(Take: take), cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        var query = BuildVectorQuery(predicate, operation, options, probeForMore: false);
        return Task.FromResult<IReadOnlyCollection<TDTOModel>>(
            query.Select(TDTOModel.Projection).ToArray());
    }

    public virtual Task<VectorPageResult<TDTOModel>> VectorPageAsync(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        var take = options.Take ?? 25;
        var skip = options.Skip ?? 0;
        var query = BuildVectorQuery(predicate, operation, options with { Take = take }, probeForMore: true);
        var values = query.Select(TDTOModel.Projection).ToArray();
        var hasMore = values.Length > take;
        IReadOnlyCollection<TDTOModel> items = hasMore ? values.Take(take).ToArray() : values;
        return Task.FromResult(new VectorPageResult<TDTOModel>(items, new VectorPageInfo(take, skip, hasMore)));
    }

    protected IQueryable<TDBModel> BuildVectorQuery(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        VectorQueryOptions options,
        bool probeForMore)
    {
        IQueryable<TDBModel> query = Security.Query<TDBModel>(operation)
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
    public virtual async Task<TDTOModel> UpdateAsync<TUpdateRequest>(TUpdateRequest model, CancellationToken cancellationToken = default) where TUpdateRequest : IUpdateDTOModel<TDBModel>
    {
        var current = await Security.GetByIdAsync<TDBModel>(model.id, SecurityOperations.Update, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id '{model.id}' does not exist or is not accessible.");
        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var updated = await Security.StageUpdateAsync(model.ApplyTo(current), ct);
            await Security.SaveChangesAsync(ct);
            return TDTOModel.FromModel(updated);
        }, cancellationToken);
    }
    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: ExecuteInTransactionAsync, StageDeleteAsync, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public virtual Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<TDBModel>(id, ct);
            await Security.SaveChangesAsync(ct);
        }, cancellationToken);
}
