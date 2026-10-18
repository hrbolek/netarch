using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.DBLayer;
using RootNamespace.EntityName.Models;

namespace RootNamespace.EntityName.Services;


public interface IEntityNameService : ISecuredEntityService<EntityNameModel>
{
}


public sealed class EntityNameService
    : SecuredEntityService<EntityNameModel>, IEntityNameService
{
    public EntityNameService(IAuthorizationLayer security, IDBLayer db) : base(security, db)
    {
    }
}
