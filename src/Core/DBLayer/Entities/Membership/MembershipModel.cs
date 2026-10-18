using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Časově omezené členství uživatele ve skupině. Je nezávislé na udělených rolích; skončení členství automaticky neruší přístupová práva z rolí.
/// </summary>

public sealed record MembershipModel(
    Guid id,
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel
{
    [NotMapped]
    public string? group_name { get; init; }

    [NotMapped]
    public string? user_name { get; init; }
}
