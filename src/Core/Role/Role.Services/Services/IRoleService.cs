using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Services;

public interface IRoleService : IBaseService<RoleModel, RoleResponse>
{
    Task<RoleResponse> CreateContainedAsync(RoleCreateContainedRequest request, CancellationToken cancellationToken = default);
}
