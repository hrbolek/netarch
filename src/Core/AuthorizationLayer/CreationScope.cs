using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>
/// Trusted authorization target used before a new secured entity exists.
/// The target is always derived server-side from a parent, type or well-known catalog scope.
/// </summary>
/// <summary>
/// Typ Creation Scope ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public readonly record struct CreationScope(Guid RbacObjectId, string Kind, Guid? SourceId = null);
