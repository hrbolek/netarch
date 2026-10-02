using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.User.Models;

namespace WebAppArchitecture.User.Services;

public interface IUserService : IBaseService<UserModel, UserResponse>
{
}

public sealed class UserService
    : BaseService<UserModel, UserResponse, IUserStore>, IUserService
{
    public UserService(IUserStore store) : base(store)
    {
    }
}

// public static class UserServiceExtensions
// {
//     public static IServiceCollection AddUserServices(this IServiceCollection services)
//     {
//         services.AddSingleton<IUserStore, InMemoryUserStore>();
//         services.AddScoped<IUserService, UserService>();
//         return services;
//     }
// }
