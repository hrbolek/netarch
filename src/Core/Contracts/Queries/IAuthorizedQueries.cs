using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Base.Queries;

/// <summary>Provides Read-authorized sources for the effective actor from the trusted context.</summary>
public interface IAuthorizedQueries
{
    /// <summary>
    /// Restricts access to the current effective actor's Read permission.
    /// Missing identity must never fall back to an unrestricted source.
    /// </summary>
    IAuthorizedQuery<TDBModel> Read<TDBModel>()
        where TDBModel : class, ISecuredEntityDBModel;
}
