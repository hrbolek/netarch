using System.Text.Json.Serialization;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Models;

/// <summary>
/// Vstupní kontrakt Role Type Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record RoleTypeUpdateRequest(
    Guid id,
    [property: JsonRequired] DateTimeOffset updated_at,
    string name,
    string[] operations
) : IUpdateDTOModel<RoleTypeModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static RoleTypeUpdateRequest FromModel(RoleTypeModel model)
        => new(model.id, model.updated_at, model.name,
            model.operation_links.Where(x => x.operation is not null).Select(x => x.operation!.name).ToArray());

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RoleTypeModel ApplyTo(RoleTypeModel model) => model with
    {
        name = name,
        requested_operations = operations.Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
    };
}
