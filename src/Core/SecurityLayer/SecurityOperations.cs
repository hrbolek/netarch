namespace WebAppArchitecture.SecurityLayer;

/// <summary>Canonical operation names used by the application security boundary.
/// The database Operation table may contain additional operations without schema changes.
/// </summary>
public static class SecurityOperations
{
    public const string Read = "read";
    public const string List = "list";
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";
}
