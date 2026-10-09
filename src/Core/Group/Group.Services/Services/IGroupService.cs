using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Services;

public interface IGroupService : IBaseService<GroupModel, GroupResponse>
{
    Task<GroupResponse> CreateContainedAsync(GroupCreateContainedRequest request, CancellationToken cancellationToken = default);
    Task<GroupResponse> CreateTypedAsync(GroupCreateTypedRequest request, CancellationToken cancellationToken = default);
}
