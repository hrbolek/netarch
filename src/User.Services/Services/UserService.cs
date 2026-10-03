using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Services;

public interface IUserService : IBaseService<UserModel, UserResponse>
{
}

public sealed class UserService
    : BaseService<UserModel, UserResponse>, IUserService
{
    public UserService(ISecurityLayer security) : base(security)
    {
    }
}
