using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

public sealed class EfRoleStore(ApplicationDbContext db) : EfStore<RoleModel>(db), IRoleStore
{
    public override async Task<RoleModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Roles.AsNoTracking().Include(x => x.role_type).SingleOrDefaultAsync(x => x.id == id, cancellationToken);

    public override async Task<IReadOnlyCollection<RoleModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Db.Roles.AsNoTracking().Include(x => x.role_type).ToArrayAsync(cancellationToken);

    public override Task<RoleModel> CreateAsync(RoleModel model, CancellationToken cancellationToken = default)
    {
        Db.Roles.Add(model);
        return Task.FromResult(model);
    }

    public override Task<RoleModel> UpdateAsync(RoleModel model, CancellationToken cancellationToken = default)
    {
        Db.Roles.Update(model);
        return Task.FromResult(model);
    }
}
