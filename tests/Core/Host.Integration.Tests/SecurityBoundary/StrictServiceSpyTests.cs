using System.Net;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Services;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class StrictServiceSpyTests
{
    [Fact]
    public async Task UnconfiguredOperationsFailAndAreRememberedEvenIfCaught()
    {
        var (service, spy) = StrictServiceSpy.Create<IUserService>();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GetByIdAsync<UserResponse>(Guid.NewGuid()));
        Assert.Single(spy.Calls);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(spy.Verify);
    }

    [Fact]
    public void UnusedExpectationsFailVerification()
    {
        var (_, spy) = StrictServiceSpy.Create<IUserService>();
        spy.Expect("DeleteAsync", _ => Task.CompletedTask);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(spy.Verify);
    }

    [Fact]
    public async Task CaughtArgumentAssertionStillFailsVerification()
    {
        var (service, spy) = StrictServiceSpy.Create<IUserService>();
        spy.Expect("DeleteAsync", _ =>
        {
            Assert.Fail("wrong authorization-sensitive argument");
            return Task.CompletedTask;
        });
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => service.DeleteAsync(Guid.NewGuid()));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(spy.Verify);
    }

    [Fact]
    public async Task DifferentOverloadIsAnUnexpectedCall()
    {
        var (service, spy) = StrictServiceSpy.Create<IUserService>();
        spy.ExpectOverload("VectorAsync", 3,
            _ => Task.FromResult<IReadOnlyCollection<UserResponse>>([]), typeof(UserResponse));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VectorAsync<UserResponse>(_ => true, "read", 10));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(spy.Verify);
    }

    [Fact]
    public void PersistenceGuardFailsResolutionAndRecordsIt()
    {
        using var host = new ResolverBoundaryHost();
        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(host.Services);
        Assert.Throws<InvalidOperationException>(() =>
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<WebAppArchitecture.DBLayer.IDBLayer>(scope.ServiceProvider));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(host.Verify);
    }
}
