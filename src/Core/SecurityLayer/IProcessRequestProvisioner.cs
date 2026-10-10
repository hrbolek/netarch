using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Narrow infrastructure writer for a single, previously authorized DigitalProcesses request.
/// It cannot create arbitrary objects in the faculty RBAC scope.
/// </summary>
public interface IProcessRequestProvisioner
{
    Task<IProcessRequestWriteScope> BeginStartAsync(Guid targetGroupId, GroupModel requestGroup, CancellationToken ct = default);
    Task<IProcessRequestWriteScope> BeginTransitionAsync(Guid requestGroupId, CancellationToken ct = default);
}

public interface IProcessRequestWriteScope
{
    Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken ct = default)
        where TEntity : class, ISecuredEntityDBModel;
}
