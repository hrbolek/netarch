namespace WebAppArchitecture.Base.UI.Routing;

public sealed record ApplicationNavigationItem(
    string Label,
    string? Url = null,
    IReadOnlyCollection<ApplicationNavigationItem>? Children = null,
    string? RequiresResponseModel = null,
    bool Separator = false
);

public sealed record ApplicationDefinition(
    string Key,
    string Label,
    string RootUrl,
    string Description,
    IReadOnlyCollection<ApplicationNavigationItem> Navigation
);

public interface IApplicationNavigationRegistry
{
    IReadOnlyCollection<ApplicationDefinition> GetApplications();
    ApplicationDefinition? Get(string key);
    void Register(ApplicationDefinition definition);
}

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

public static class ArchitectureNavigationRegistration
{
    public static ApplicationNavigationRegistry RegisterArchitecture(this ApplicationNavigationRegistry registry)
    {
        registry.Register(new ApplicationDefinition(
            Key: "architecture",
            Label: "Architecture",
            RootUrl: "/architecture",
            Description: "Core entities, organizational structure and RBAC administration.",
            Navigation:
            [
                new("User", Children:
                [
                    new("Seznam", "/architecture/UserResponse/list"),
                    new("Vytvořit", "/architecture/UserResponse/create"),
                    new("", Separator: true),
                    new("Memberships", "/architecture/UserResponse/memberships/{id}", RequiresResponseModel: "UserResponse"),
                    new("Roles", "/architecture/UserResponse/roles/{id}", RequiresResponseModel: "UserResponse")
                ]),
                new("Group", Children:
                [
                    new("Seznam", "/architecture/GroupResponse/list"),
                    new("Vytvořit", "/architecture/GroupResponse/create"),
                    new("", Separator: true),
                    new("Memberships", "/architecture/GroupResponse/memberships/{id}", RequiresResponseModel: "GroupResponse"),
                    new("Roles", "/architecture/GroupResponse/roles/{id}", RequiresResponseModel: "GroupResponse")
                ]),
                new("Role", Children:
                [
                    new("Seznam", "/architecture/RoleResponse/list"),
                    new("Vytvořit", "/architecture/RoleResponse/create")
                ]),
                new("Membership", Children:
                [
                    new("Seznam", "/architecture/MembershipResponse/list"),
                    new("Vytvořit", "/architecture/MembershipResponse/create")
                ]),
                new("RoleType", Children:
                [
                    new("Seznam", "/architecture/RoleTypeResponse/list"),
                ])
            ]));

        return registry;
    }
}
