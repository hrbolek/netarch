using System.Linq.Expressions;

namespace WebAppArchitecture.Base.Data;

/// <summary>
/// Minimal contract shared by persistence and generic UI components.
/// The lowercase id intentionally follows the existing WebAppArchitecture models.
/// </summary>

public interface IUpdateDTOModel<TEntity>: IEntityDTOModel
    where TEntity : IEntityDBModel
{
    /// <summary>The original server-issued timestamp, used only to detect concurrent updates.</summary>
    DateTimeOffset updated_at { get; }
    TEntity ApplyTo(TEntity model);
}
