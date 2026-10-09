using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

public interface ISecurityService : IRbacService
{
    Task<IReadOnlySet<string>> GetAllowedOperationsAsync(Guid userId, Guid rbacId, CancellationToken cancellationToken = default);
}
