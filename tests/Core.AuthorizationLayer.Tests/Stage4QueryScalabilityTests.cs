using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;
using AuthorizationLayerImpl = WebAppArchitecture.AuthorizationLayer.AuthorizationLayer;
using Xunit;

namespace Core.AuthorizationLayer.Tests;

/// <summary>
/// T14: provider-neutral, deterministic scalability regressions. These are not
/// throughput benchmarks; provider comparisons belong exclusively to Stage 99.
/// </summary>
public sealed class Stage4QueryScalabilityTests
{
    private static AuthorizationLayerImpl Layer(RbacFixture fixture, Guid user)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["x-ms-client-principal-id"] = user.ToString();
        var actor = new HeaderOrClaimActorProvider();
        return new AuthorizationLayerImpl(fixture.Db, new RbacAuthorizationService(fixture.Db),
            actor, new HttpContextAccessor { HttpContext = http });
    }

    [Fact]
    public async Task T14_SecureQuery_RemainsComposedBeforeExecution()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, SecurityOperations.Read);
        f.Deny(f.Secret, f.ApplicantType);
        await f.Sync();

        // ToQueryString requires a translatable relational query but does not
        // execute it. The caller's predicate remains inside the same query.
        var q = Layer(f, f.Student).Query<GroupModel>(SecurityOperations.Read)
            .Where(g => g.id == f.Visible);
        Assert.False(string.IsNullOrWhiteSpace(q.ToQueryString()));
        Assert.Equal(f.Visible, await q.Select(g => g.id).SingleAsync());
        Assert.Empty(await Layer(f, f.Student).Query<GroupModel>(SecurityOperations.Read)
            .Where(g => g.id == f.Secret).ToListAsync());
    }

    [Fact]
    public async Task T14_ManySecuredRows_KeepDenyAndCallerPagingCorrect()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, SecurityOperations.Read);
        var newIds = Enumerable.Range(0, 80).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var id in newIds)
            f.AddGroup(id, f.Request, $"Secured-{id:N}");
        f.Context.SaveChanges();
        f.Deny(f.Secret, f.ApplicantType);
        await f.Sync();

        var query = Layer(f, f.Student).Query<GroupModel>(SecurityOperations.Read);
        Assert.Equal(80, await query.CountAsync(g => newIds.Contains(g.id)));
        var page = await query.Where(g => newIds.Contains(g.id))
            .OrderBy(g => g.id).Skip(20).Take(10).Select(g => g.id).ToArrayAsync();
        Assert.Equal(10, page.Length);
        Assert.Equal(10, page.Distinct().Count());
        Assert.DoesNotContain(f.Secret, await query.Select(g => g.id).ToArrayAsync());
    }

    [Fact]
    public async Task T14_MultipleRoles_DoesNotDuplicateAuthorizedEntityRows()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.AddRole(f.Student, f.Request, f.ReaderType);
        f.Operation(f.ApplicantType, SecurityOperations.Read);
        f.Operation(f.ReaderType, SecurityOperations.Read);
        await f.Sync();

        var query = Layer(f, f.Student).Query<GroupModel>(SecurityOperations.Read);
        Assert.Equal(1, await query.CountAsync(g => g.id == f.Visible));
        Assert.Equal(f.Visible, await query.Where(g => g.id == f.Visible)
            .Select(g => g.id).SingleAsync());
    }
}
