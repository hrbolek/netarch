using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Filtering;
using RootNamespace.EntityName.Models;
using RootNamespace.EntityName.Services;

namespace RootNamespace.EntityName.Resolvers;


public static class EntityNameResolvers
{
    public static Task<IResult> Read(Guid id, HttpContext http, IEntityNameService service, CancellationToken ct)
        => BaseResolvers.Read<EntityNameModel, EntityNameResponse, IEntityNameService>(id, http, service, ct);

    public static Task<IResult> List(HttpContext http, IEntityNameService service, CancellationToken ct)
        => BaseResolvers.List<EntityNameModel, EntityNameResponse, IEntityNameService>(http, service, ct);

    public static Task<IResult> Update(EntityNameUpdateRequest body, HttpContext http, IEntityNameService service, CancellationToken ct)
        => BaseResolvers.Update<EntityNameModel, EntityNameResponse, EntityNameUpdateRequest, IEntityNameService>(body, http, service, ct);

    public static Task<IResult> Delete(Guid id, HttpContext http, IEntityNameService service, CancellationToken ct)
        => BaseResolvers.Delete<EntityNameModel, EntityNameResponse, IEntityNameService>(id, http, service, ct);

    public static Task<IResult> Link(Guid id, HttpContext http, IEntityNameService service, CancellationToken ct)
        => BaseResolvers.Link<EntityNameModel, EntityNameResponse, IEntityNameService>(id, http, service, ct);

    // Create is domain-owned: validate business invariants and register RBAC object in a transaction.
}

// HTTP mapping is intentionally separate from the thin resolver methods.
// Canonical route prefix is supplied by the module host, NOT duplicated here.
