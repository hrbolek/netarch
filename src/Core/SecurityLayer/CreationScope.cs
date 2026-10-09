using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Trusted authorization target used before a new secured entity exists.
/// The target is always derived server-side from a parent, type or well-known catalog scope.
/// </summary>
public readonly record struct CreationScope(Guid RbacObjectId, string Kind, Guid? SourceId = null);
