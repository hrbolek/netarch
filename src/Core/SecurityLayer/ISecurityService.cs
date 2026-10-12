using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Služba I Security Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface ISecurityService : IRbacService
{
    Task<IReadOnlySet<string>> GetAllowedOperationsAsync(Guid userId, Guid rbacId, CancellationToken cancellationToken = default);
}
