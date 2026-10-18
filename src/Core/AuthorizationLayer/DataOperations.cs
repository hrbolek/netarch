using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>Internal EF persistence adapter. No identity or authorization. Never register as a public service.</summary>
internal sealed class DataOperations<TDBModel> where TDBModel : class, ISecuredEntityDBModel
{
    private readonly IDBLayer _db;
    internal DataOperations(IDBLayer db) => _db = db;
    internal IQueryable<TDBModel> Query() => _db.Query<TDBModel>();
    internal Task<TDBModel?> ReadAsync(Guid id, CancellationToken ct = default) => _db.GetByIdAsync<TDBModel>(id, ct);
    internal Task<TDBModel> StageCreateAsync(TDBModel model, CancellationToken ct = default) => _db.StageCreateAsync(model, ct);
    internal Task<TDBModel> StageUpdateAsync(TDBModel model, CancellationToken ct = default) => _db.StageUpdateAsync(model, ct);
    internal Task StageDeleteAsync(Guid id, CancellationToken ct = default) => _db.StageDeleteAsync<TDBModel>(id, ct);
}
