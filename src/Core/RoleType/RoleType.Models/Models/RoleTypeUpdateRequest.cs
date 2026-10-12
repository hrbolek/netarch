using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Models;

/// <summary>
/// Vstupní kontrakt Role Type Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record RoleTypeUpdateRequest(
    Guid id,
    string name,
    string[] operations,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<RoleTypeModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static RoleTypeUpdateRequest FromModel(RoleTypeModel model)
        => new(model.id, model.name,
            model.operation_links.Where(x => x.operation is not null).Select(x => x.operation!.name).ToArray(),
            model.updated_at, model.updated_by);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RoleTypeModel ApplyTo(RoleTypeModel model) => model with
    {
        name = name,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by,
        requested_operations = operations.Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
    };

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RoleTypeModel ToModel() => new(id, name, RbacWellKnown.RootSecurityObjectId,
        DateTimeOffset.UtcNow, updated_at ?? DateTimeOffset.UtcNow, Guid.Empty, updated_by ?? Guid.Empty)
    {
        requested_operations = operations
    };
}
