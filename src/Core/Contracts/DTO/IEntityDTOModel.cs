using System.Linq.Expressions;

namespace WebAppArchitecture.Base.Data;

/// <summary>
/// Minimal contract shared by persistence and generic UI components.
/// The lowercase id intentionally follows the existing WebAppArchitecture models.
/// </summary>

public interface IEntityDTOModel
{
    Guid id { get; }
    string visual_label => id.ToString();
}
