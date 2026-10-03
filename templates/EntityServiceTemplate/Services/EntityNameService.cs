using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using RootNamespace.EntityName.Models;

namespace RootNamespace.EntityName.Services;

public interface IEntityNameService : IBaseService<EntityNameModel, EntityNameResponse>
{
}

public sealed class EntityNameService
    : BaseService<EntityNameModel, EntityNameResponse>, IEntityNameService
{
    public EntityNameService(ISecurityLayer security) : base(security)
    {
    }
}
