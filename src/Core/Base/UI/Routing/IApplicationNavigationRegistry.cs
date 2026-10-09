namespace WebAppArchitecture.Base.UI.Routing;

public interface IApplicationNavigationRegistry
{
    IReadOnlyCollection<ApplicationDefinition> GetApplications();
    ApplicationDefinition? Get(string key);
    void Register(ApplicationDefinition definition);
}
