using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.Base.UI.Pages;

public sealed record EntityRelationsViewModel<TParent, TChild>(
    TParent Parent,
    IReadOnlyCollection<TChild> Items,
    string Title,
    string SectionTitle,
    VectorPageInfo? PageInfo = null
) : IEntityRelationsViewModel<TParent>;
