using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Group.Models;

namespace WebAppArchitecture.Group.Services;

public interface IGroupService : IBaseService<GroupModel, GroupResponse>
{
}

public sealed class GroupService : BaseService<GroupModel, GroupResponse, IGroupStore>, IGroupService
{
    public GroupService(IGroupStore store) : base(store)
    {
    }
}
