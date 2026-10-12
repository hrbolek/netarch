using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Katalog pojmenovaných bezpečnostních operací používaných při vyhodnocení efektivního přístupu.
/// </summary>
public sealed record OperationModel(
    Guid id,
    string name,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDBModel;

/// <summary>Normalized many-to-many mapping RoleType -> Operation.</summary>
