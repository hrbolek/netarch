using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using Xunit;

namespace Core.AuthorizationLayer.Tests;

public sealed class ActorIdentitySourceTests
{
    [Fact]
    public void CompatibilityOperationNamesMatchCanonicalRbacNames()
    {
        Assert.Equal(RbacOperations.Read, WebAppArchitecture.AuthorizationLayer.SecurityOperations.Read);
        Assert.Equal(RbacOperations.List, WebAppArchitecture.AuthorizationLayer.SecurityOperations.List);
        Assert.Equal(RbacOperations.Create, WebAppArchitecture.AuthorizationLayer.SecurityOperations.Create);
        Assert.Equal(RbacOperations.Update, WebAppArchitecture.AuthorizationLayer.SecurityOperations.Update);
        Assert.Equal(RbacOperations.Delete, WebAppArchitecture.AuthorizationLayer.SecurityOperations.Delete);
        Assert.Equal(RbacOperations.StartProcess, WebAppArchitecture.AuthorizationLayer.SecurityOperations.StartProcess);
        Assert.Equal(RbacOperationIds.Read, RbacOperationIds.FromName(" READ "));
        Assert.Throws<ArgumentOutOfRangeException>(() => RbacOperationIds.FromName("unknown-operation"));
    }

    [Fact]
    public void QueryStringActorUserId_IsNeverTrusted()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?actorUserId=" + Guid.NewGuid());
        Assert.Null(new HeaderOrClaimActorProvider().TryGetActorUserId(context));
    }

    [Fact]
    public void QueryStringActorUserId_DoesNotOverrideTrustedClaim()
    {
        var claimedId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?actorUserId=" + Guid.NewGuid());
        context.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("oid", claimedId.ToString()) }, "test"));
        Assert.Equal(claimedId, new HeaderOrClaimActorProvider().TryGetActorUserId(context));
    }
}
