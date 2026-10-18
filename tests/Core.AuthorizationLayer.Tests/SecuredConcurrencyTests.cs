using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Services;
using Xunit;

namespace Core.AuthorizationLayer.Tests;

public sealed class SecuredConcurrencyTests
{
    [Fact]
    public async Task AuthorizedWritesDeriveAuditWithoutTrustingIncomingModelMetadata()
    {
        using var fixture = new RbacFixture();
        fixture.Operation(fixture.ReaderType, RbacOperations.Create);
        fixture.Operation(fixture.ReaderType, RbacOperations.Update);
        fixture.AddRole(fixture.Student, fixture.Request, fixture.ReaderType);
        await fixture.Sync();
        var security = Security(fixture);
        var forgedTime = DateTimeOffset.UtcNow.AddYears(1);
        var input = new UserModel(Guid.NewGuid(), "Created", null, null, fixture.Request,
            forgedTime, forgedTime, fixture.Dean, fixture.Dean);
        var created = await security.StageCreateAsync(input);
        await security.SaveChangesAsync();
        Assert.Equal(fixture.Student, created.created_by);
        Assert.Equal(fixture.Student, created.updated_by);
        Assert.Equal(created.created_at, created.updated_at);
        Assert.True(created.created_at < forgedTime);
        var createdAt = created.created_at;
        var token = created.updated_at;
        var updated = await security.StageUpdateAsync(created with
        {
            Name = "Updated", created_at = forgedTime, updated_at = forgedTime,
            created_by = fixture.Dean, updated_by = fixture.Dean
        }, token);
        await security.SaveChangesAsync();
        Assert.Equal(createdAt, updated.created_at);
        Assert.Equal(fixture.Student, updated.created_by);
        Assert.Equal(fixture.Student, updated.updated_by);
        Assert.True(updated.updated_at > token);
        Assert.True(updated.updated_at < forgedTime);
    }

    [Fact]
    public async Task UnpagedVectorsRemainUnboundedAndPagesKeepStableOrderingAndAuthorization()
    {
        using var fixture = new RbacFixture();
        fixture.Operation(fixture.ReaderType, RbacOperations.List);
        fixture.AddRole(fixture.Student, fixture.Request, fixture.ReaderType);
        for (var index = 0; index < 31; index++)
            fixture.Context.Users.Add(new UserModel(Guid.NewGuid(), $"Vector {index:D2}", null, null,
                fixture.Request, fixture.Now, fixture.Now, fixture.Student, fixture.Student));
        fixture.Context.Users.Add(new UserModel(Guid.NewGuid(), "Vector forbidden", null, null,
            fixture.Root, fixture.Now, fixture.Now, fixture.Student, fixture.Student));
        await fixture.Context.SaveChangesAsync();
        await fixture.Sync();
        var service = new UserService(Security(fixture), fixture.Db, fixture.Projection);
        var options = new VectorQueryOptions(OrderBy: [new VectorOrderBy("Name")]);
        var all = await service.VectorAsync<UserResponse>(
            model => model.Name.StartsWith("Vector "), RbacOperations.List, options);
        Assert.Equal(31, all.Count);
        Assert.DoesNotContain(all, item => item.Name == "Vector forbidden");
        var first = await service.VectorPageAsync<UserResponse>(
            model => model.Name.StartsWith("Vector "), RbacOperations.List, options);
        Assert.Equal(25, first.Items.Count);
        Assert.True(first.PageInfo.HasMore);
        var second = await service.VectorPageAsync<UserResponse>(
            model => model.Name.StartsWith("Vector "), RbacOperations.List, options with { Skip = 25 });
        Assert.Equal(6, second.Items.Count);
        Assert.False(second.PageInfo.HasMore);
        Assert.Equal(all.Select(item => item.id),
            first.Items.Concat(second.Items).Select(item => item.id));
    }

    private static IAuthorizationLayer Security(RbacFixture fixture)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-ms-client-principal-id"] = fixture.Student.ToString();
        return new WebAppArchitecture.AuthorizationLayer.AuthorizationLayer(
            fixture.Db, new RbacAuthorizationService(fixture.Db), new HeaderOrClaimActorProvider(),
            new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public async Task TwoWritersWithSameTokenCannotOverwriteEachOtherAndRollbackClearsFailedWork()
    {
        using var fixture = new RbacFixture();
        using var services = new ServiceCollection().BuildServiceProvider();
        await using var firstContext = fixture.CreateContext();
        await using var secondContext = fixture.CreateContext();
        var first = new DBLayer(firstContext, services);
        var second = new DBLayer(secondContext, services);
        var original = (await first.GetByIdAsync<UserModel>(fixture.Student))!;
        var secondSnapshot = (await second.GetByIdAsync<UserModel>(fixture.Student))!;

        var firstResult = await first.StageAuditedUpdateAsync(original with
        {
            Name = "First writer", created_by = Guid.NewGuid(), updated_by = Guid.NewGuid()
        }, original, original.updated_at, fixture.Dean);
        await first.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            second.ExecuteInTransactionAsync(async ct =>
            {
                await second.StageAuditedUpdateAsync(secondSnapshot with { Name = "Second writer" },
                    secondSnapshot, secondSnapshot.updated_at, fixture.Student, ct);
                await second.SaveChangesAsync(ct);
            }));
        Assert.Empty(secondContext.ChangeTracker.Entries());

        var persisted = (await second.GetByIdAsync<UserModel>(fixture.Student))!;
        Assert.Equal("First writer", persisted.Name);
        Assert.Equal(original.created_at, persisted.created_at);
        Assert.Equal(original.created_by, persisted.created_by);
        Assert.Equal(fixture.Dean, persisted.updated_by);
        Assert.True(persisted.updated_at > original.updated_at);
        Assert.Equal(firstResult.updated_at, persisted.updated_at);
        await second.SaveChangesAsync();
        Assert.Equal("First writer", (await first.GetByIdAsync<UserModel>(fixture.Student))!.Name);

        var retry = await second.StageAuditedUpdateAsync(persisted with { Name = "Fresh retry" },
            persisted, persisted.updated_at, fixture.Student);
        await second.SaveChangesAsync();
        Assert.True(retry.updated_at > persisted.updated_at);
        Assert.Equal("Fresh retry", (await first.GetByIdAsync<UserModel>(fixture.Student))!.Name);
    }
}
