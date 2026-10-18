using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.Base.UI.Pages;


public interface IEntityRelationsViewModel<out TParent>
{
    TParent Parent { get; }
    string Title { get; }
    string SectionTitle { get; }
}
