namespace WebAppArchitecture.Base.UI.Routing;

public static class ArchitectureNavigationRegistration
{
    public static IApplicationNavigationRegistry RegisterArchitecture(this IApplicationNavigationRegistry registry)
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
