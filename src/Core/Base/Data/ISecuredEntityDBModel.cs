using System.Linq.Expressions;

namespace WebAppArchitecture.Base.Data;

/// <summary>
/// Minimal contract shared by persistence and generic UI components.
/// The lowercase id intentionally follows the existing WebAppArchitecture models.
/// </summary>
public interface ISecuredEntityDBModel : IEntityDBModel
{
    Guid rbacobject_id { get; }
    DateTimeOffset created_at { get; }
    DateTimeOffset updated_at { get; }
    Guid created_by { get; }
    Guid updated_by { get; }
}
