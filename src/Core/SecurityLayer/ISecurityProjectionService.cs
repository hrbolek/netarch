using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

public interface ISecurityProjectionService
{
    Task RebuildAllAsync(CancellationToken cancellationToken = default);
    Task SynchronizeRbacObjectAsync(Guid rbacObjectId, CancellationToken cancellationToken = default);
    Task SynchronizeRbacObjectsAsync(IEnumerable<Guid> rbacObjectIds, CancellationToken cancellationToken = default);
    Task SynchronizeRoleImpactAsync(IEnumerable<Guid> sourceGroupIds, CancellationToken cancellationToken = default);
    Task SynchronizeGroupSubtreeAsync(Guid rootGroupId, CancellationToken cancellationToken = default);
}
