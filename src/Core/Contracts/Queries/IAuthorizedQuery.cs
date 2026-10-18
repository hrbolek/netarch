using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Base.Queries;

/// <summary>
/// Composes and materializes an already authorized entity source without exposing its provider.
/// Query operations must preserve the source's authorization restrictions.
/// </summary>
public interface IAuthorizedQuery<TDBModel>
    where TDBModel : class, ISecuredEntityDBModel
{
    IAuthorizedQuery<TDBModel> Where(Expression<Func<TDBModel, bool>> predicate);
    IAuthorizedQuery<TDBModel> OrderBy<TKey>(Expression<Func<TDBModel, TKey>> selector);
    IAuthorizedQuery<TDBModel> OrderByDescending<TKey>(Expression<Func<TDBModel, TKey>> selector);
    IAuthorizedQuery<TDBModel> Skip(int count);
    IAuthorizedQuery<TDBModel> Take(int count);
    Task<List<TDBModel>> ToListAsync(CancellationToken ct = default);
    Task<TDBModel?> FirstOrDefaultAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
}
