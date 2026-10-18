using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebAppArchitecture.Facilities;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class FacilitiesResolverBoundaryTests
{
    private static readonly Guid Actor = Guid.Parse("61000000-0000-0000-0000-000000000001");
    private static readonly Guid Id = Guid.Parse("62000000-0000-0000-0000-000000000001");
    private static readonly Guid Scope = Guid.Parse("63000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Token = DateTimeOffset.Parse("2026-10-18T10:00:00Z");
    private const string Facilities = "/facilities/FacilityResponse";
    private const string Types = "/facilities/FacilityTypeResponse";
    private const string Reservations = "/facilities/FacilityReservationResponse";

    private static FacilityResponse Facility() => FacilityResponse.FromModel(new FacilityModel
    {
        id = Id, Name = "Room", rbacobject_id = Scope, updated_at = Token
    });

    private static ReservationResponse Reservation() => ReservationResponse.FromModel(new ReservationModel
    {
        id = Id, FacilityId = Id, EventId = Scope, rbacobject_id = Scope,
        StartsAt = Token, EndsAt = Token.AddHours(1), updated_at = Token
    });

    private static HttpRequestMessage Request(HttpMethod method, string uri, object? body = null)
    {
        var request = ActingRequest.Get(uri, Actor);
        request.Method = method;
        request.Headers.Add("X-Actor-UserId", Guid.Empty.ToString());
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    [Theory]
    [InlineData("/list")]
    [InlineData("/tree")]
    public async Task ListsUseOnlyTheirDesignatedServiceAndTrustedActor(string route)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect("ListAsync", call =>
        {
            Assert.Equal(Actor, call.Actor);
            Assert.Equal("room", call.Arguments[0]);
            Assert.Equal(2, call.Arguments[1]);
            Assert.Equal(7, call.Arguments[2]);
            return Task.FromResult<IReadOnlyList<FacilityResponse>>([]);
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, Facilities + route + "?search=room&skip=2&take=7&actor_user_id=" + Guid.Empty);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        host.Verify();
    }

    [Theory]
    [InlineData("/facilities/FacilityResponse/list")]
    [InlineData("/facilities/FacilityResponse/tree")]
    [InlineData("/facilities/FacilityTypeResponse/list")]
    public async Task CallerSelectedOperationIsRejectedWithoutServiceCalls(string route)
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, route + "?operation=create");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData("children", "ListChildrenAsync")]
    [InlineData("ancestors", "ListAncestorsAsync")]
    [InlineData("descendants", "ListDescendantsAsync")]
    public async Task TreeNavigationDelegatesTheBoundParent(string route, string method)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect(method, call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Actor, call.Actor);
            return Task.FromResult<IReadOnlyList<FacilityResponse>>([]);
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, $"{Facilities}/{route}/{Id}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreatePreservesTypedInputAndNeverTurnsDenialIntoSuccess(bool denied)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect("CreateAsync", call =>
        {
            var input = Assert.IsType<FacilityCreateRequest>(call.Arguments[0]);
            Assert.Equal("Room", input.Name);
            Assert.Equal(Scope, input.RbacObjectId);
            Assert.Equal(Actor, call.Actor);
            if (denied)
                throw new UnauthorizedAccessException("denied");
            return Task.FromResult(Facility());
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Post, Facilities + "/create", new
        {
            name = "Room", code = "R1", facilityTypeId = Id, rbacObjectId = Scope,
            isActive = true, capacity = 20, actor_user_id = Guid.Empty,
            created_by = Guid.Empty, updated_by = Guid.Empty
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(denied ? HttpStatusCode.Forbidden : HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateForwardsConcurrencyTokenAndServiceConflict(bool conflict)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect("UpdateAsync", call =>
        {
            var input = Assert.IsType<FacilityUpdateRequest>(call.Arguments[0]);
            Assert.Equal(Id, input.id);
            Assert.Equal(Token, input.updated_at);
            Assert.Equal(Actor, call.Actor);
            if (conflict)
                throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("stale");
            return Task.FromResult(Facility());
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Put, Facilities + "/update", new
        {
            id = Id, updated_at = Token, name = "Room", code = "R1", capacity = 20,
            facilityTypeId = Scope, isActive = true, updated_by = Guid.Empty
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(conflict ? HttpStatusCode.Conflict : HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task RouteBodyMismatchStopsUpdateBeforeService()
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Put, $"{Facilities}/update/{Id}", new
        {
            id = Scope, updated_at = Token, name = "Room", code = "R1",
            facilityTypeId = Scope, isActive = true
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task DeleteCallsOnlyTheTokenBearingServiceMethod()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().ExpectOverload("DeleteAsync", 3, call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Token, Assert.IsType<FacilityDeleteRequest>(call.Arguments[1]).updated_at);
            Assert.Equal(Actor, call.Actor);
            throw new UnauthorizedAccessException("denied");
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Delete, $"{Facilities}/delete/{Id}", new FacilityDeleteRequest(Token));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MoveDelegatesDestinationAndTokenWithoutChoosingRbacOperations(bool denied)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect("MoveAsync", call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            var input = Assert.IsType<FacilityMoveRequest>(call.Arguments[1]);
            Assert.Equal(Scope, input.ParentId);
            Assert.Equal(Token, input.updated_at);
            Assert.Equal(Actor, call.Actor);
            if (denied)
                throw new UnauthorizedAccessException("move denied");
            return Task.FromResult(Facility());
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Post, $"{Facilities}/move/{Id}", new
        {
            parentId = Scope, updated_at = Token, operation = "read",
            rbacObjectId = Guid.Empty, actor_user_id = Guid.Empty
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(denied ? HttpStatusCode.Forbidden : HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task TypeCreateUsesOnlyTypeService()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityTypeService>().Expect("CreateAsync", call =>
        {
            var input = Assert.IsType<FacilityTypeCreateRequest>(call.Arguments[0]);
            Assert.Equal("ROOM", input.Code);
            Assert.True(input.Reservable);
            Assert.Equal(Scope, input.RbacObjectId);
            Assert.Equal(Actor, call.Actor);
            return Task.FromResult(FacilityTypeResponse.FromModel(new FacilityTypeModel
            {
                id = Id, Code = "ROOM", Name = "Room", rbacobject_id = Scope
            }));
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Post, Types + "/create", new
        {
            code = "ROOM", name = "Room", reservable = true, canContainChildren = false,
            allowedChildTypeIds = Array.Empty<Guid>(), rbacObjectId = Scope
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task TypeUpdateAndDeleteUseConstrainedTokenBearingMethods()
    {
        using var host = new ResolverBoundaryHost();
        var spy = host.Spy<IFacilityTypeService>();
        spy.Expect("UpdateAsync", call =>
        {
            var input = Assert.IsType<FacilityTypeUpdateRequest>(call.Arguments[0]);
            Assert.Equal(Id, input.id);
            Assert.Equal(Token, input.updated_at);
            Assert.Equal(Actor, call.Actor);
            throw new UnauthorizedAccessException("update denied");
        });
        spy.ExpectOverload("DeleteAsync", 3, call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Token, Assert.IsType<FacilityDeleteRequest>(call.Arguments[1]).updated_at);
            throw new UnauthorizedAccessException("delete denied");
        });
        using var client = host.CreateClient();
        using var update = Request(HttpMethod.Put, Types + "/update", new
        {
            id = Id, updated_at = Token, code = "ROOM", name = "Room",
            reservable = true, canContainChildren = false, allowedChildTypeIds = Array.Empty<Guid>()
        });
        using var updated = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.Forbidden, updated.StatusCode);
        using var delete = Request(HttpMethod.Delete, $"{Types}/delete/{Id}", new FacilityDeleteRequest(Token));
        using var deleted = await client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.Forbidden, deleted.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task TypeReadUsesSecuredProjectionAndHidesMissingObjects()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityTypeService>().Expect("GetByIdAsync", call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Actor, call.Actor);
            return Task.FromResult<FacilityTypeResponse?>(null);
        }, typeof(FacilityTypeResponse));
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, $"{Types}/view/{Id}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task TypeRouteBodyMismatchStopsUpdateBeforeService()
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Put, $"{Types}/update/{Id}", new
        {
            id = Scope, updated_at = Token, code = "ROOM", name = "Room",
            reservable = true, canContainChildren = false, allowedChildTypeIds = Array.Empty<Guid>()
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FacilityReadUsesOnlySecuredProjectionAndHidesMissingObjects(bool missing)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect("GetByIdAsync", call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Actor, call.Actor);
            return Task.FromResult<FacilityResponse?>(missing ? null : Facility());
        }, typeof(FacilityResponse));
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, $"{Facilities}/view/{Id}");
        using var response = await client.SendAsync(request);
        Assert.Equal(missing ? HttpStatusCode.NotFound : HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task UnexpectedServiceFailureCannotBecomeSuccessfulResponse()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect("GetByIdAsync", _ =>
            throw new IOException("controlled service failure"), typeof(FacilityResponse));
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, $"{Facilities}/view/{Id}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData("confirm", "ConfirmAsync")]
    [InlineData("reject", "RejectAsync")]
    [InlineData("cancel", "CancelAsync")]
    public async Task ReservationTransitionsUseOnlyDesignatedService(string route, string method)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IReservationService>().Expect(method, call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            var input = Assert.IsType<ReservationTransitionRequest>(call.Arguments[1]);
            Assert.Equal(Token, input.updated_at);
            Assert.Equal("reason", input.Reason);
            Assert.Equal(Actor, call.Actor);
            return Task.FromResult(Reservation());
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Post, $"{Reservations}/{route}/{Id}", new
        {
            updated_at = Token, reason = "reason", status = "Confirmed", actor_user_id = Guid.Empty
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task ReservationRequestDoesNotAcceptCallerSelectedStatusOrActor()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IReservationService>().Expect("RequestAsync", call =>
        {
            var input = Assert.IsType<ReservationRequest>(call.Arguments[0]);
            Assert.Equal(Id, input.FacilityId);
            Assert.Equal(Scope, input.EventId);
            Assert.Equal(Token, input.StartsAt);
            Assert.Equal(Token.AddHours(1), input.EndsAt);
            Assert.Equal(Actor, call.Actor);
            throw new UnauthorizedAccessException("event update denied");
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Post, Reservations + "/request", new
        {
            facilityId = Id, eventId = Scope, startsAt = Token, endsAt = Token.AddHours(1),
            rbacObjectId = Scope, status = "Confirmed", actor_user_id = Guid.Empty
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData("by-facility", "ListByFacilityAsync")]
    [InlineData("by-event", "ListByEventAsync")]
    public async Task ReservationRelationsUseBoundIdentifiers(string route, string method)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IReservationService>().Expect(method, call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Actor, call.Actor);
            return Task.FromResult<IReadOnlyList<ReservationResponse>>([]);
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, $"{Reservations}/{route}/{Id}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task EventFacilitiesAssociationUsesOnlyReservationService()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IReservationService>().Expect("ListFacilitiesByEventAsync", call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Actor, call.Actor);
            return Task.FromResult<IReadOnlyList<FacilityResponse>>([]);
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, $"{Reservations}/facilities-by-event/{Id}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        host.Verify();
    }

    [Fact]
    public async Task AvailabilityForwardsHalfOpenIntervalToService()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IReservationService>().Expect("AvailabilityAsync", call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Token, call.Arguments[1]);
            Assert.Equal(Token.AddHours(1), call.Arguments[2]);
            return Task.FromResult(new FacilityAvailabilityResponse(Id, Token, Token.AddHours(1), false));
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get,
            $"{Reservations}/availability/{Id}?startsAt={Uri.EscapeDataString(Token.ToString("O"))}&endsAt={Uri.EscapeDataString(Token.AddHours(1).ToString("O"))}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<FacilityAvailabilityResponse>())!.IsAvailable);
        host.Verify();
    }

    [Fact]
    public async Task OccupancyDelegatesIntervalAndPagingWithoutDirectPersistence()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IReservationService>().Expect("OccupancyAsync", call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            Assert.Equal(Token, call.Arguments[1]);
            Assert.Equal(Token.AddHours(1), call.Arguments[2]);
            Assert.Equal(2, call.Arguments[3]);
            Assert.Equal(7, call.Arguments[4]);
            Assert.Equal(Actor, call.Actor);
            return Task.FromResult<IReadOnlyList<ReservationResponse>>([]);
        });
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get,
            $"{Reservations}/occupancy/{Id}?startsAt={Uri.EscapeDataString(Token.ToString("O"))}&endsAt={Uri.EscapeDataString(Token.AddHours(1).ToString("O"))}&skip=2&take=7");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        host.Verify();
    }

    [Theory]
    [InlineData("/facilities/FacilityResponse/list")]
    [InlineData("/facilities/FacilityTypeResponse/list")]
    [InlineData("/facilities/FacilityResponse/tree")]
    public async Task AnonymousListsNeverInvokeDomainServices(string route)
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync(route);
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 401, received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        host.Verify();
    }

    [Fact]
    public async Task EmptyListRendersThroughRazorWithoutPersistence()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect("ListAsync", _ => Task.FromResult<IReadOnlyList<FacilityResponse>>([]));
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, Facilities + "/list");
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("text/html");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Facility", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        host.Verify();
    }

    [Theory]
    [InlineData("/facilities/FacilityResponse/new")]
    [InlineData("/facilities/FacilityReservationResponse/new")]
    public async Task CreateFormsRenderThroughRazorWithoutPersistence(string route)
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, route);
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("text/html");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<form", await response.Content.ReadAsStringAsync());
        host.Verify();
    }

    [Theory]
    [InlineData("detail")]
    [InlineData("edit")]
    public async Task FacilityDetailAndEditRenderOnlySecuredServiceModels(string page)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFacilityService>().Expect("GetByIdAsync", call =>
        {
            Assert.Equal(Id, call.Arguments[0]);
            return Task.FromResult<FacilityResponse?>(Facility());
        }, typeof(FacilityResponse));
        if (page == "detail")
        {
            host.Spy<IFacilityService>().Expect("ListChildrenAsync", _ =>
                Task.FromResult<IReadOnlyList<FacilityResponse>>([]));
            host.Spy<IReservationService>().Expect("ListByFacilityAsync", _ =>
                Task.FromResult<IReadOnlyList<ReservationResponse>>([]));
        }
        else
        {
            host.Spy<IFacilityTypeService>().Expect("ListAsync", _ =>
                Task.FromResult<IReadOnlyList<FacilityTypeResponse>>([]));
        }
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, $"{Facilities}/{page}/{Id}");
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("text/html");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Room", await response.Content.ReadAsStringAsync());
        host.Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TypeAdministrationFormsRenderOnlySecuredModels(bool edit)
    {
        using var host = new ResolverBoundaryHost();
        if (edit)
            host.Spy<IFacilityTypeService>().Expect("GetByIdAsync", _ =>
                Task.FromResult<FacilityTypeResponse?>(FacilityTypeResponse.FromModel(new FacilityTypeModel
                {
                    id = Id, Name = "Room type", Code = "ROOM", rbacobject_id = Scope, updated_at = Token
                })), typeof(FacilityTypeResponse));
        host.Spy<IFacilityTypeService>().Expect("ListAsync", _ =>
            Task.FromResult<IReadOnlyList<FacilityTypeResponse>>([]));
        using var client = host.CreateClient();
        using var request = Request(HttpMethod.Get, Types + (edit ? $"/edit/{Id}" : "/new"));
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("text/html");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<form", await response.Content.ReadAsStringAsync());
        host.Verify();
    }

    [Fact]
    public async Task OpenApiDocumentsFacilitiesOperationsAndRepresentations()
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var operations = paths.EnumerateObject()
            .Where(path => path.Name.StartsWith("/facilities/", StringComparison.Ordinal))
            .SelectMany(path => path.Value.EnumerateObject()
                .Where(operation => operation.Name is "get" or "post" or "put" or "delete")
                .Select(operation => operation.Value))
            .ToArray();
        Assert.NotEmpty(operations);
        foreach (var operation in operations)
        {
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("operationId").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.NotEmpty(operation.GetProperty("tags").EnumerateArray());
        }
        Assert.Contains(operations, operation => operation.GetProperty("operationId").GetString() == "FacilityMove");
        Assert.Contains(operations, operation => operation.GetProperty("operationId").GetString() == "FacilityReservationConfirm");
        var content = paths.GetProperty(Facilities + "/list").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content");
        Assert.True(content.TryGetProperty("application/json", out _));
        Assert.True(content.TryGetProperty("text/html", out _));
        host.Verify();
    }
}
