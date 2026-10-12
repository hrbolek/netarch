using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using Xunit;

namespace Core.SecurityLayer.Tests;

public sealed class ActorIdentitySourceTests
{
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
