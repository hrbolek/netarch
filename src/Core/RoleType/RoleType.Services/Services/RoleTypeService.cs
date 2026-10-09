using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.RoleType.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Services;

public sealed class RoleTypeService : BaseService<RoleTypeModel, RoleTypeResponse>, IRoleTypeService
{
    public RoleTypeService(ISecurityLayer security) : base(security)
    {
    }
}
