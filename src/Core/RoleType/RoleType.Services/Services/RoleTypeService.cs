using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.RoleType.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Services;

/// <summary>
/// Služba Role Type Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class RoleTypeService : BaseService<RoleTypeModel, RoleTypeResponse>, IRoleTypeService
{
    public RoleTypeService(ISecurityLayer security) : base(security)
    {
    }
}
