using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Role.Models;

namespace WebAppArchitecture.Role.Services;

public interface IRoleService : IBaseService<RoleModel, RoleResponse>
{
}

public sealed class RoleService : BaseService<RoleModel, RoleResponse, IRoleStore>, IRoleService
{
    public RoleService(IRoleStore store) : base(store)
    {
    }
}
