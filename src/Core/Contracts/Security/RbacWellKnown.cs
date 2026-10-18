namespace WebAppArchitecture.Base.Security;

public static class RbacWellKnown
{
    public static readonly Guid RootGroupId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid RootSecurityObjectId = RootGroupId;
    public static readonly Guid RootGroupTypeId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid BootstrapAdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // Dedicated creation scopes. They are ordinary RBAC Groups created by DatabaseInitializer.
    // Public create endpoints authorize against one of these existing objects instead of
    // accepting an arbitrary rbacobject_id from the client.
    public static readonly Guid FormDefinitionCreationScopeId = Guid.Parse("11000000-0000-0000-0000-000000000001");
    public static readonly Guid WorkflowDefinitionCreationScopeId = Guid.Parse("11000000-0000-0000-0000-000000000002");
    public static readonly Guid ProcessDefinitionCreationScopeId = Guid.Parse("11000000-0000-0000-0000-000000000003");
    public static readonly Guid FormNodeTypeCreationScopeId = Guid.Parse("11000000-0000-0000-0000-000000000004");
}
