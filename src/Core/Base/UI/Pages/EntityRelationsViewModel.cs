using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.Base.UI.Pages;

public interface IEntityRelationsViewModel<out TParent>
{
    TParent Parent { get; }
    string Title { get; }
    string SectionTitle { get; }
}

public sealed record EntityRelationsViewModel<TParent, TChild>(
    TParent Parent,
    IReadOnlyCollection<TChild> Items,
    string Title,
    string SectionTitle,
    VectorPageInfo? PageInfo = null
) : IEntityRelationsViewModel<TParent>;
