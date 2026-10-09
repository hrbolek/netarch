namespace WebAppArchitecture.Base.UI.Routing;

public sealed class ApplicationNavigationRegistry : IApplicationNavigationRegistry
{
    private readonly Dictionary<string, ApplicationDefinition> _definitions =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ApplicationDefinition> GetApplications()
        => _definitions.Values.OrderBy(item => item.Label).ToArray();

    public ApplicationDefinition? Get(string key)
        => _definitions.TryGetValue(key, out var definition) ? definition : null;

    public void Register(ApplicationDefinition definition)
        => _definitions[definition.Key] = definition;
}
