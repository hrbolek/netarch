namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>Canonical operation names used by the application security boundary.
/// The database Operation table may contain additional operations without schema changes.
/// </summary>
public static class SecurityOperations
{
    public const string StartProcess = WebAppArchitecture.Base.Security.RbacOperations.StartProcess;
    public const string Read = WebAppArchitecture.Base.Security.RbacOperations.Read;
    public const string List = WebAppArchitecture.Base.Security.RbacOperations.List;
    public const string Create = WebAppArchitecture.Base.Security.RbacOperations.Create;
    public const string Update = WebAppArchitecture.Base.Security.RbacOperations.Update;
    public const string Delete = WebAppArchitecture.Base.Security.RbacOperations.Delete;
}
