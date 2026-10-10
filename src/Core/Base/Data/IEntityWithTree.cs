namespace WebAppArchitecture.Base.Data;

/// <summary>
/// Read-only structural contract for entities stored with a materialized path.
/// Implementations retain their own mapped properties; this interface does not
/// impose EF inheritance, path encoding, or database-specific operations.
/// </summary>
public interface IEntityWithTree : IEntityDBModel
{
    string path { get; }
}
