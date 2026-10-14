using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IDigitalProcessActionService<TEntity> where TEntity : class, ISecuredEntityDBModel
{
    Task<bool> CanReadAsync(Guid id, CancellationToken ct = default);
}

public sealed class DigitalProcessActionService<TEntity>(ISecurityLayer security, IDBLayer db)
    : SecuredBaseService<TEntity>(security, db), IDigitalProcessActionService<TEntity>
    where TEntity : class, ISecuredEntityDBModel
{
    public async Task<bool> CanReadAsync(Guid id, CancellationToken ct = default)
        => await Security.GetByIdAsync<TEntity>(id, SecurityOperations.Read, ct) is not null;
}
