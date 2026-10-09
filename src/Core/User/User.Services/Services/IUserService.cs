using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Services;

public interface IUserService : IBaseService<UserModel, UserResponse>
{
    Task<UserResponse> CreateContainedAsync(UserCreateContainedRequest request, CancellationToken cancellationToken = default);
}
