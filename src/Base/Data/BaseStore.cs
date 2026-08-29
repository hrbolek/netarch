using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace WebAppArchitecture.Base.Data;

public interface IBaseStore<TModel>
    where TModel : IEntityDBModel
{
    Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TModel>> VectorAsync(
        Expression<Func<TModel, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default);
    Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Demo store persistent only for the lifetime of the process.
/// </summary>
public class InMemoryStore<TModel> : IBaseStore<TModel>
    where TModel : IEntityDBModel
{
    private readonly ConcurrentDictionary<Guid, TModel> _store = new();

    public Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var model);
        return Task.FromResult(model);
    }

    public Task<IReadOnlyCollection<TModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TModel> result = _store.Values.ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<TModel>> VectorAsync(
        Expression<Func<TModel, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var compiled = predicate.Compile();
        IReadOnlyCollection<TModel> result = _store.Values.Where(compiled).ToArray();
        return Task.FromResult(result);
    }

    public Task<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        if (!_store.TryAdd(model.id, model))
            throw new InvalidOperationException($"Item with id '{model.id}' already exists.");

        return Task.FromResult(model);
    }

    public Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        if (!_store.ContainsKey(model.id))
            throw new KeyNotFoundException($"Item with id '{model.id}' was not found.");

        _store[model.id] = model;
        return Task.FromResult(model);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
