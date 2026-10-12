using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Services;

/// <summary>
/// Služba I Role Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IRoleService : IBaseService<RoleModel>
{
    Task<RoleResponse> CreateContainedAsync(RoleCreateContainedRequest request, CancellationToken cancellationToken = default);
}
