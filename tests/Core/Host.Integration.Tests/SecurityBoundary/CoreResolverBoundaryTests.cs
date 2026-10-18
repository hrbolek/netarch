using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.Group.Services;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.Tests.Shared;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Services;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class CoreResolverBoundaryTests
{
    private static readonly Guid Actor = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Id = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Scope = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Token = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    public static IEnumerable<object[]> Entities()
    {
        yield return ["User"];
        yield return ["Group"];
        yield return ["Role"];
        yield return ["Membership"];
    }

    public static IEnumerable<object[]> Decisions()
        => Entities().SelectMany(entity => new[] { new object[] { entity[0], true }, new object[] { entity[0], false } });

    private static Type UpdateDto(string entity) => entity switch
    {
        "User" => typeof(UserUpdateRequest),
        "Group" => typeof(GroupUpdateRequest),
        "Role" => typeof(RoleUpdateRequest),
        "Membership" => typeof(MembershipUpdateRequest),
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };

    private static StrictServiceSpy EntitySpy(ResolverBoundaryHost host, string entity) => entity switch
    {
        "User" => host.Spy<IUserService>(),
        "Group" => host.Spy<IGroupService>(),
        "Role" => host.Spy<IRoleService>(),
        "Membership" => host.Spy<IMembershipService>(),
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };

    private static Type Dto(string entity) => entity switch
    {
        "User" => typeof(UserResponse),
        "Group" => typeof(GroupResponse),
        "Role" => typeof(RoleResponse),
        "Membership" => typeof(MembershipResponse),
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };

    private static object ReadResult(string entity, bool missing) => entity switch
    {
        "User" => Task.FromResult(missing ? null : User()),
        "Group" => Task.FromResult(missing ? null : new GroupResponse(Id, "group", Scope, null, "/", true, Scope, Token, Token, Actor, Actor)),
        "Role" => Task.FromResult(missing ? null : new RoleResponse(Id, Scope, Actor, Scope, null, false, Token, null, null, Scope, Token, Token, Actor, Actor)),
        "Membership" => Task.FromResult(missing ? null : new MembershipResponse(Id, Scope, Actor, Token, null, Scope, Token, Token, Actor, Actor)),
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };

    private static UserResponse User() => new(Id, "visible", null, null, Scope, Token, Token, Actor, Actor);

    private static object Page(string entity) => entity switch
    {
        "User" => Task.FromResult(new VectorPageResult<UserResponse>([], new(7, 0, false))),
        "Group" => Task.FromResult(new VectorPageResult<GroupResponse>([], new(7, 0, false))),
        "Role" => Task.FromResult(new VectorPageResult<RoleResponse>([], new(7, 0, false))),
        "Membership" => Task.FromResult(new VectorPageResult<MembershipResponse>([], new(7, 0, false))),
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };

    [Theory]
    [MemberData(nameof(Entities))]
    public async Task ListsUseDesignatedServiceAndFixedReadOperation(string entity)
    {
        using var host = new ResolverBoundaryHost();
        var spy = EntitySpy(host, entity);
        spy.Expect("VectorPageAsync", call =>
        {
            Assert.Equal(Actor, call.Actor);
            Assert.Equal(RbacOperations.Read, call.Arguments[1]);
            Assert.Equal(7, Assert.IsType<VectorQueryOptions>(call.Arguments[2]).Take);
            return Page(entity);
        }, Dto(entity));
        using var client = host.CreateClient();
        using var request = ActingRequest.Get($"/architecture/{entity}Response/list?take=7&actor_user_id={Id}", Actor);
        request.Headers.Add("X-Actor-UserId", Id.ToString());
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        Assert.Single(spy.Calls);
        host.Verify();
    }

    [Theory]
    [MemberData(nameof(Entities))]
    public async Task CallerCannotChooseListOperationAndRejectedInputCallsNoService(string entity)
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        using var request = ActingRequest.Get($"/architecture/{entity}Response/list?operation=create", Actor);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [MemberData(nameof(Entities))]
    public async Task ReadAndLinkUseOnlyTheirDesignatedService(string entity)
    {
        using var host = new ResolverBoundaryHost();
        var spy = EntitySpy(host, entity);
        using var client = host.CreateClient();
        foreach (var operation in new[] { "view", "edit", "link" })
        foreach (var missing in new[] { false, true })
        {
            spy.Expect("GetByIdAsync", call =>
            {
                Assert.Equal(Id, call.Arguments[0]);
                Assert.Equal(Actor, call.Actor);
                return ReadResult(entity, missing);
            }, Dto(entity));
            using var request = ActingRequest.Get($"/architecture/{entity}Response/{operation}/{Id}", Actor);
            using var response = await client.SendAsync(request);
            Assert.Equal(missing ? HttpStatusCode.NotFound : HttpStatusCode.OK, response.StatusCode);
        }
        Assert.Equal(6, spy.Calls.Count);
        host.Verify();
    }

    [Theory]
    [MemberData(nameof(Entities))]
    public async Task MissingOrUnreadableObjectStopsDeleteBeforeAuthorizationOrMutation(string entity)
    {
        using var host = new ResolverBoundaryHost();
        EntitySpy(host, entity).Expect("GetByIdAsync", _ => ReadResult(entity, missing: true), Dto(entity));
        using var client = host.CreateClient();
        using var request = ActingRequest.Get($"/architecture/{entity}Response/delete?id={Id}", Actor);
        request.Method = HttpMethod.Delete;
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [MemberData(nameof(Entities))]
    public async Task ServiceExceptionsNeverBecomeSuccessfulResponses(string entity)
    {
        using var host = new ResolverBoundaryHost();
        EntitySpy(host, entity).Expect("GetByIdAsync", _ => throw new InvalidOperationException("service failed"), Dto(entity));
        using var client = host.CreateClient();
        using var request = ActingRequest.Get($"/architecture/{entity}Response/view/{Id}", Actor);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task CreateTargetsRemainFixedToCreate()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IGroupService>().Expect("VectorPageAsync", call =>
        {
            Assert.Equal(RbacOperations.Create, call.Arguments[1]);
            return Page("Group");
        }, typeof(GroupResponse));
        using var client = host.CreateClient();
        using var request = ActingRequest.Get("/architecture/GroupResponse/create-targets", Actor);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var bad = ActingRequest.Get("/architecture/GroupResponse/create-targets?operation=read", Actor);
        using var rejected = await client.SendAsync(bad);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        host.Verify();
    }

    [Theory]
    [MemberData(nameof(Decisions))]
    public async Task UpdatePreservesTokenAndTrustedActorDespiteHostileInput(string entity, bool allowed)
    {
        using var host = new ResolverBoundaryHost();
        var spy = EntitySpy(host, entity);
        spy.Expect("GetByIdAsync", _ => ReadResult(entity, missing: false), Dto(entity));
        host.Spy<IRbacAuthorizationService>().Expect("IsAllowedAsync", call =>
        {
            Assert.Equal(Actor, call.Arguments[0]);
            Assert.Equal(Scope, call.Arguments[1]);
            Assert.Equal(RbacOperations.Update, call.Arguments[2]);
            return Task.FromResult(allowed);
        });
        if (allowed)
            spy.Expect("UpdateAsync", call =>
        {
            var input = Assert.IsAssignableFrom<IEntityDTOModel>(call.Arguments[0]);
            Assert.Equal(UpdateDto(entity), input.GetType());
            Assert.Equal(Id, input.id);
            var token = input switch
            {
                UserUpdateRequest user => user.updated_at,
                GroupUpdateRequest group => group.updated_at,
                RoleUpdateRequest role => role.updated_at,
                MembershipUpdateRequest membership => membership.updated_at,
                _ => throw new InvalidOperationException("Unexpected update DTO")
            };
            Assert.Equal(Token, token);
            Assert.Equal(Actor, call.Actor);
            return ReadResult(entity, missing: false);
        }, UpdateDto(entity), Dto(entity));
        using var client = host.CreateClient();
        using var request = ActingRequest.Get($"/architecture/{entity}Response/update?operation=delete&actor_user_id={Id}", Actor);
        request.Method = HttpMethod.Put;
        request.Content = JsonContent.Create(new { id = Id, Name = "new", group_type_id = Scope, role_type_id = Scope, valid_from = Token, updated_at = Token, updated_by = Id, created_by = Id, actor_user_id = Id });
        using var response = await client.SendAsync(request);
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.InternalServerError, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task MissingActorOrTokenStopsUpdateBeforeService()
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        using var anonymous = new HttpRequestMessage(HttpMethod.Put, "/architecture/UserResponse/update")
        {
            Content = JsonContent.Create(new UserUpdateRequest(Id, Token, "anonymous"))
        };
        using var denied = await client.SendAsync(anonymous);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var invalid = ActingRequest.Get("/architecture/UserResponse/update", Actor);
        invalid.Method = HttpMethod.Put;
        invalid.Content = JsonContent.Create(new UserUpdateRequest(Id, default, "bad token"));
        using var rejected = await client.SendAsync(invalid);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        host.Verify();
    }

    [Theory]
    [MemberData(nameof(Decisions))]
    public async Task DeleteUsesFixedOperationAndStopsMutationWhenDenied(string entity, bool allowed)
    {
        using var host = new ResolverBoundaryHost();
        var spy = EntitySpy(host, entity);
        spy.Expect("GetByIdAsync", _ => ReadResult(entity, missing: false), Dto(entity));
        host.Spy<IRbacAuthorizationService>().Expect("IsAllowedAsync", call =>
        {
            Assert.Equal(Actor, call.Arguments[0]);
            Assert.Equal(Scope, call.Arguments[1]);
            Assert.Equal(RbacOperations.Delete, call.Arguments[2]);
            return Task.FromResult(allowed);
        });
        if (allowed)
            spy.Expect("DeleteAsync", call =>
            {
                Assert.Equal(Id, call.Arguments[0]);
                return Task.CompletedTask;
            });
        using var client = host.CreateClient();
        using var request = ActingRequest.Get($"/architecture/{entity}Response/delete?id={Id}&operation=read&user_id={Id}", Actor);
        request.Method = HttpMethod.Delete;
        using var response = await client.SendAsync(request);
        // Existing host has no authentication handler to execute Results.Forbid.
        // This characterizes the HTTP translation gap, not a successful denial contract.
        Assert.Equal(allowed ? HttpStatusCode.NoContent : HttpStatusCode.InternalServerError, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [MemberData(nameof(Decisions))]
    public async Task CreateContainedDelegatesTypedInputAndNeverTurnsDenialIntoSuccess(string entity, bool allowed)
    {
        using var host = new ResolverBoundaryHost();
        EntitySpy(host, entity).Expect("CreateContainedAsync", call =>
        {
            Assert.Equal(Actor, call.Actor);
            var target = call.Arguments[0] switch
            {
                UserCreateContainedRequest user => user.group_id,
                GroupCreateContainedRequest group => group.master_group_id,
                RoleCreateContainedRequest role => role.group_id,
                MembershipCreateContainedRequest membership => membership.group_id,
                _ => throw new InvalidOperationException("Unexpected create DTO")
            };
            Assert.Equal(Scope, target);
            Assert.DoesNotContain(call.Arguments[0]!.GetType().GetProperties(), property =>
                property.Name is "actor_user_id" or "rbacobject_id" or "created_by" or "updated_by");
            return allowed ? ReadResult(entity, missing: false) : throw new UnauthorizedAccessException("service denied create");
        });
        using var client = host.CreateClient();
        using var request = ActingRequest.Get($"/architecture/{entity}Response/create-contained?operation=read&actor_user_id={Id}", Actor);
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new
        {
            Name = "created", group_id = Scope, master_group_id = Scope, user_id = Id, role_type_id = Scope,
            valid_from = Token, actor_user_id = Id, created_by = Id, rbacobject_id = Id
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.InternalServerError, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StructuredVectorUsesFixedVectorOperationAndRequiresActor(bool authenticated)
    {
        using var host = new ResolverBoundaryHost();
        if (authenticated)
            host.Spy<IUserService>().ExpectOverload("VectorAsync", 3, call =>
            {
                Assert.Equal(RbacOperations.Vector, call.Arguments[1]);
                Assert.Equal(Actor, call.Actor);
                return Task.FromResult<IReadOnlyCollection<UserResponse>>([]);
            }, typeof(UserResponse));
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/architecture/UserResponse/vector?operation=create&actorUserId=" + Id)
        {
            Content = JsonContent.Create(new { Where = new { }, actor_user_id = Id })
        };
        if (authenticated)
            request.Headers.Add("x-ms-client-principal-id", Actor.ToString());
        using var response = await client.SendAsync(request);
        Assert.Equal(authenticated ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task CustomMembershipVectorBindsParentPredicateAndReadOperation()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IUserService>().Expect("GetByIdAsync", _ => Task.FromResult<UserResponse?>(User()), typeof(UserResponse));
        host.Spy<IMembershipService>().Expect("VectorPageAsync", call =>
        {
            Assert.Equal(RbacOperations.Read, call.Arguments[1]);
            var predicate = Assert.IsAssignableFrom<Expression<Func<WebAppArchitecture.DBLayer.MembershipModel, bool>>>(call.Arguments[0]).Compile();
            var model = new WebAppArchitecture.DBLayer.MembershipModel(Guid.NewGuid(), Scope, Id, Token, null, Scope, Token, Token, Actor, Actor);
            Assert.True(predicate(model));
            Assert.False(predicate(model with { user_id = Guid.NewGuid() }));
            return Page("Membership");
        }, typeof(MembershipResponse));
        using var client = host.CreateClient();
        using var request = ActingRequest.Get($"/architecture/UserResponse/memberships/{Id}?user_id={Actor}", Actor);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }
}
