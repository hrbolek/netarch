using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.RoleType.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Services;

public interface IRoleTypeService : IBaseService<RoleTypeModel, RoleTypeResponse>
{
}
