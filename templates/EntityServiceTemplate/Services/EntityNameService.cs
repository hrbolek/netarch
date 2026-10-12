using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.DBLayer;
using RootNamespace.EntityName.Models;

namespace RootNamespace.EntityName.Services;

public interface IEntityNameService : IBaseService<EntityNameModel>
{
}

public sealed class EntityNameService
    : SecuredBaseService<EntityNameModel>, IEntityNameService
{
    public EntityNameService(ISecurityLayer security, IDBLayer db) : base(security, db)
    {
    }
}
