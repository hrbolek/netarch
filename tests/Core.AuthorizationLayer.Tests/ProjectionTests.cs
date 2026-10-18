using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using AuthorizationLayerImpl = WebAppArchitecture.AuthorizationLayer.AuthorizationLayer;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;
using Xunit;
namespace Core.AuthorizationLayer.Tests;

/// <summary>T02: hierarchy/materialized paths; T03: operations; T06: bindings; T07: projection; T08: deny; T09: secure SQL.</summary>
public sealed class ProjectionTests
{
    [Theory]
    [InlineData("root",true)] [InlineData("faculty",true)] [InlineData("request",true)]
    [InlineData("secret",true)] [InlineData("secretChild",true)] [InlineData("otherFaculty",true)]
    public async Task T02_Role_AtRoot_FlowsDown(string target,bool expected)
    {
        using var f=new RbacFixture();f.AddRole(f.Dean,f.Root,f.DeanType);await f.Sync();
        Assert.Equal(expected,f.Has(f.Dean,Target(f,target),f.DeanType));
    }
    [Theory]
    [InlineData("root",false)] [InlineData("faculty",false)] [InlineData("request",true)]
    [InlineData("visible",true)] [InlineData("secret",true)] [InlineData("otherFaculty",false)]
    public async Task T02_Role_AtRequest_FlowsToDescendantsOnly(string target,bool expected)
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);await f.Sync();
        Assert.Equal(expected,f.Has(f.Student,Target(f,target),f.ApplicantType));
    }
    [Fact] public async Task T02_BreakInheritance_DoesNotRemoveLocalRole()
    {
        using var f=new RbacFixture();f.AddRole(f.Dean,f.Faculty,f.DeanType);f.AddRole(f.Student,f.Secret,f.ApplicantType);
        var g=f.Context.Groups.Single(x=>x.id==f.Secret);f.Context.Entry(g).CurrentValues.SetValues(g with {inherit_roles=false});f.Context.SaveChanges();await f.Sync();
        Assert.False(f.Has(f.Dean,f.Secret,f.DeanType));Assert.False(f.Has(f.Dean,f.SecretChild,f.DeanType));Assert.True(f.Has(f.Student,f.SecretChild,f.ApplicantType));
    }
    [Theory] [InlineData(true,true)] [InlineData(false,false)]
    public async Task T08_Deny_IsRecursiveOnlyWhenRequested(bool recursive,bool childHidden)
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Deny(f.Secret,f.ApplicantType,recursive);await f.Sync();
        Assert.False(f.Has(f.Student,f.Secret,f.ApplicantType));Assert.Equal(!childHidden,f.Has(f.Student,f.SecretChild,f.ApplicantType));
        Assert.True(f.Has(f.Student,f.Visible,f.ApplicantType));Assert.True(f.Has(f.Student,f.Sibling,f.ApplicantType));
    }
    [Fact] public async Task T08_DenyApplicant_DoesNotDenyDean()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.AddRole(f.Dean,f.Faculty,f.DeanType);f.Deny(f.Secret,f.ApplicantType);await f.Sync();
        Assert.False(f.Has(f.Student,f.SecretChild,f.ApplicantType));Assert.True(f.Has(f.Dean,f.SecretChild,f.DeanType));
    }
    [Fact] public async Task T08_Deny_CoversLocallyAssignedRoleAndCanBeRemoved()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Secret,f.ApplicantType);var id=f.Deny(f.Secret,f.ApplicantType);await f.Sync();Assert.False(f.Has(f.Student,f.Secret,f.ApplicantType));
        f.Context.RbacDenyRules.Remove(f.Context.RbacDenyRules.Single(x=>x.id==id));f.Context.SaveChanges();await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);Assert.True(f.Has(f.Student,f.Secret,f.ApplicantType));
    }
    [Fact] public async Task T06_Binding_ChangesRoleTypeAndDenyMatchesSourceRole()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Bind(f.Request,f.ApplicantType,f.Secret,f.ReaderType);f.Deny(f.Secret,f.ApplicantType);await f.Sync();
        Assert.False(f.Has(f.Student,f.Secret,f.ReaderType));Assert.True(f.Has(f.Student,f.Visible,f.ApplicantType));
    }
    [Fact] public async Task T06_Binding_AllowsProjectionToNonInheritedTarget()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Bind(f.Request,f.ApplicantType,f.OtherFaculty,f.ReaderType);await f.Sync();
        Assert.True(f.Has(f.Student,f.OtherFaculty,f.ReaderType));Assert.False(f.Has(f.Student,f.OtherFaculty,f.ApplicantType));
    }
    [Fact] public async Task T07_RemoveLastRole_RevokesEffectiveAccess()
    {
        using var f=new RbacFixture();var id=f.AddRole(f.Student,f.Request,f.ApplicantType);await f.Sync();Assert.True(f.Has(f.Student,f.Visible,f.ApplicantType));
        f.Context.Roles.Remove(f.Context.Roles.Single(x=>x.id==id));f.Context.SaveChanges();await f.Projection.SynchronizeGroupSubtreeAsync(f.Request);
        Assert.False(f.Has(f.Student,f.Visible,f.ApplicantType));
    }
    [Fact] public async Task T07_OneOfTwoSourcesRemoved_RetainsAccess()
    {
        using var f=new RbacFixture();var id=f.AddRole(f.Student,f.Request,f.ApplicantType);f.AddRole(f.Student,f.Faculty,f.ApplicantType);await f.Sync();
        f.Context.Roles.Remove(f.Context.Roles.Single(x=>x.id==id));f.Context.SaveChanges();await f.Projection.SynchronizeRoleImpactAsync(new[]{f.Request,f.Faculty});
        Assert.True(f.Has(f.Student,f.Visible,f.ApplicantType));
    }
    [Fact] public async Task T07_Rebuild_IsIdempotent()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);await f.Sync();
        var first=f.Context.EffectiveAccesses.AsNoTracking().Select(x=>new{x.user_id,x.rbacobject_id,x.role_type_id,x.source_role_id}).ToArray();await f.Sync();
        var second=f.Context.EffectiveAccesses.AsNoTracking().Select(x=>new{x.user_id,x.rbacobject_id,x.role_type_id,x.source_role_id}).ToArray();
        Assert.Equal(first.OrderBy(x=>x.rbacobject_id).Select(x=>x.ToString()),second.OrderBy(x=>x.rbacobject_id).Select(x=>x.ToString()));
    }
    [Fact] public async Task T07_ExpiredRole_IsNotProjectedAsActive()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType,f.Now.AddDays(-3),f.Now.AddDays(-1));await f.Sync();Assert.False(f.Has(f.Student,f.Request,f.ApplicantType));
    }
    [Theory] [InlineData("read",true)] [InlineData("update",false)]
    public async Task T03_And_T09_SecurityService_RespectsOperations(string operation,bool expected)
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Operation(f.ApplicantType,"read");await f.Sync();
        var service=new RbacAuthorizationService(f.Db);Assert.Equal(expected,await service.IsAllowedAsync(f.Student,f.Visible,operation));
    }
    [Fact] public async Task T09_AuthorizationLayer_Query_ExecutesSQLAndFiltersRows()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Operation(f.ApplicantType,"read");f.Deny(f.Secret,f.ApplicantType);await f.Sync();
        var http=new DefaultHttpContext();http.Request.Headers["x-ms-client-principal-id"]=f.Student.ToString();
        var accessor=new HttpContextAccessor {HttpContext=http};var actor=new HeaderOrClaimActorProvider();
        var layer=new AuthorizationLayerImpl(f.Db,new RbacAuthorizationService(f.Db),actor,accessor);
        var ids=layer.Query<GroupModel>("read").Select(g=>g.id).ToArray();
        Assert.Contains(f.Request,ids);Assert.Contains(f.Visible,ids);Assert.DoesNotContain(f.Secret,ids);Assert.DoesNotContain(f.SecretChild,ids);
        Assert.DoesNotContain(f.OtherFaculty,ids);
    }
    [Fact] public async Task T08_NewDescendant_InheritsExistingDenyAfterSynchronization()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Deny(f.Secret,f.ApplicantType);await f.Sync();
        var child=Guid.NewGuid();f.AddGroup(child,f.Secret,"NewSection");f.Context.SaveChanges();await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);Assert.False(f.Has(f.Student,child,f.ApplicantType));
    }
    [Fact] public async Task T07_TargetedProjection_EqualsFullRebuild_AfterDenyAndRoleChange()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.AddRole(f.Dean,f.Faculty,f.DeanType);await f.Sync();
        f.Deny(f.Secret,f.ApplicantType);await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);
        var before=Signatures(f);
        await f.Sync();var after=Signatures(f);
        Assert.Equal(after,before);
    }
    [Fact] public async Task T07_TargetedProjection_EqualsFullRebuild_AfterRemovingDeny()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);var id=f.Deny(f.Secret,f.ApplicantType);await f.Sync();
        f.Context.RbacDenyRules.Remove(f.Context.RbacDenyRules.Single(x=>x.id==id));f.Context.SaveChanges();
        await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);var before=Signatures(f);
        await f.Sync();Assert.Equal(Signatures(f),before);
    }
    [Fact] public async Task T08_AnotherRoleStillAllowsAccess_SelectiveDenyNotGlobalBan()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.AddRole(f.Student,f.Request,f.ReaderType);
        f.Deny(f.Secret,f.ApplicantType);await f.Sync();
        Assert.False(f.Has(f.Student,f.Secret,f.ApplicantType));Assert.True(f.Has(f.Student,f.Secret,f.ReaderType));
    }
    [Fact] public async Task T08_NonRecursiveDeny_DoesNotBlockGrandchild()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Deny(f.Secret,f.ApplicantType,false);await f.Sync();
        Assert.False(f.Has(f.Student,f.Secret,f.ApplicantType));Assert.True(f.Has(f.Student,f.SecretChild,f.ApplicantType));
    }
    [Fact] public async Task T09_ForbiddenRead_DoesNotRevealEntityViaCount()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Operation(f.ApplicantType,"read");f.Deny(f.Secret,f.ApplicantType);await f.Sync();
        var actor=new HeaderOrClaimActorProvider();var context=new DefaultHttpContext();context.Request.Headers["x-ms-client-principal-id"]=f.Student.ToString();
        var layer=new AuthorizationLayerImpl(f.Db,new RbacAuthorizationService(f.Db),actor,new HttpContextAccessor{HttpContext=context});
        Assert.Equal(3,layer.Query<GroupModel>().Count()); // Request, Visible and Sibling
    }
    [Fact] public async Task T09_MissingIdentity_IsRejectedBeforeQuery()
    {
        using var f=new RbacFixture();await f.Sync();var actor=new HeaderOrClaimActorProvider();
        var layer=new AuthorizationLayerImpl(f.Db,new RbacAuthorizationService(f.Db),actor,new HttpContextAccessor{HttpContext=new DefaultHttpContext()});
        Assert.Throws<UnauthorizedAccessException>(() => { _ = layer.Query<GroupModel>().ToArray(); });
    }
    [Fact] public async Task T03_RemoveRoleOperation_RevokesReadWithoutProjectionRebuild()
    {
        using var f=new RbacFixture();f.AddRole(f.Student,f.Request,f.ApplicantType);f.Operation(f.ApplicantType,"read");await f.Sync();
        var security=new RbacAuthorizationService(f.Db);Assert.True(await security.IsAllowedAsync(f.Student,f.Request,"read"));
        f.Context.RoleTypeOperations.RemoveRange(f.Context.RoleTypeOperations.Where(x=>x.role_type_id==f.ApplicantType));f.Context.SaveChanges();
        Assert.False(await security.IsAllowedAsync(f.Student,f.Request,"read"));
    }
    private static string[] Signatures(RbacFixture f) => f.Context.EffectiveAccesses.AsNoTracking().AsEnumerable()
        .Where(x=>x.valid_to_unix_ms is null || x.valid_to_unix_ms>=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        .Select(x=>$"{x.user_id}:{x.rbacobject_id}:{x.role_type_id}:{x.source_role_id}:{x.source_access_binding_id}")
        .OrderBy(x=>x,StringComparer.Ordinal).ToArray();
    private static Guid Target(RbacFixture f,string key)=>key switch {"root"=>f.Root,"faculty"=>f.Faculty,"request"=>f.Request,"visible"=>f.Visible,"secret"=>f.Secret,"secretChild"=>f.SecretChild,"otherFaculty"=>f.OtherFaculty,"sibling"=>f.Sibling,_=>throw new ArgumentOutOfRangeException(nameof(key))};
}
