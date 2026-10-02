using WebAppArchitecture.Base.Services;
using WebAppArchitecture.RoleType.Models;

namespace WebAppArchitecture.RoleType.Services;

public interface IRoleTypeService : IBaseService<RoleTypeModel, RoleTypeResponse>
{
}

public sealed class RoleTypeService : BaseService<RoleTypeModel, RoleTypeResponse, IRoleTypeStore>, IRoleTypeService
{
    public RoleTypeService(IRoleTypeStore store) : base(store)
    {
    }
}
