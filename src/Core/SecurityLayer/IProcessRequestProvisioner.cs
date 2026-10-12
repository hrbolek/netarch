using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Narrow infrastructure writer for a single, previously authorized DigitalProcesses request.
/// It cannot create arbitrary objects in the faculty RBAC scope.
/// </summary>
/// <summary>
/// Vstupní kontrakt I Process Request Provisioner. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public interface IProcessRequestProvisioner
{
    Task<IProcessRequestWriteScope> BeginStartAsync(Guid targetGroupId, GroupModel requestGroup, CancellationToken ct = default);
    Task<IProcessRequestWriteScope> BeginTransitionAsync(Guid requestGroupId, CancellationToken ct = default);
}

/// <summary>
/// Vstupní kontrakt I Process Request Write Scope. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public interface IProcessRequestWriteScope
{
    Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken ct = default)
        where TEntity : class, ISecuredEntityDBModel;
}
