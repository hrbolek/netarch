namespace WebAppArchitecture.Base.UI.Pages;

public sealed record EntityRelationsViewModel<TParent, TChild>(
    TParent Parent,
    TChild[] Items,
    string Title,
    string SectionTitle
);
