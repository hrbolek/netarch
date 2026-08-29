using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Services;
using RootNamespace.EntityName.Models;

namespace RootNamespace.EntityName.Services;

public interface IEntityNameService : IBaseService<EntityNameModel, EntityNameResponse>
{
}

public sealed class EntityNameService
    : BaseService<EntityNameModel, EntityNameResponse, IEntityNameStore>, IEntityNameService
{
    public EntityNameService(IEntityNameStore store) : base(store)
    {
    }
}

// public static class EntityNameServiceExtensions
// {
//     public static IServiceCollection AddEntityNameServices(this IServiceCollection services)
//     {
//         services.AddSingleton<IEntityNameStore, InMemoryEntityNameStore>();
//         services.AddScoped<IEntityNameService, EntityNameService>();
//         return services;
//     }
// }
