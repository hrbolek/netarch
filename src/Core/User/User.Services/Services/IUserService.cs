using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Services;

/// <summary>
/// Služba I User Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IUserService : IBaseService<UserModel>
{
    Task<UserResponse> CreateContainedAsync(UserCreateContainedRequest request, CancellationToken cancellationToken = default);
}
