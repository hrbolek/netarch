namespace WebAppArchitecture.Base.Data;

/// <summary>
/// Minimal representation used by /link/{id} endpoints.
/// The target entity name is carried separately because the response itself
/// is a generic UI DTO rather than the linked domain DTO type.
/// </summary>

public sealed record EntityLinkResponse(
    Guid id,
    string visual_label,
    string entity,
    string? app = null,
    string? operation = null
) : IEntityDTOModel;
