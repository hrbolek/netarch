using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

/// <summary>
/// Vstupní kontrakt Group Create Typed Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record GroupCreateTypedRequest(
    string Name,
    Guid group_type_id,
    bool inherit_roles = true);
