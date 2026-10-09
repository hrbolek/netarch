using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Services;

public sealed class GroupTypeService : BaseService<GroupTypeModel, GroupTypeResponse>, IGroupTypeService
{
    public GroupTypeService(ISecurityLayer security) : base(security) { }
}
