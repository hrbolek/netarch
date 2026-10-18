using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class GenericListBoundaryTests
{
    [Fact]
    public async Task EveryRegisteredGenericListUsesReadAndRejectsCallerChosenOperation()
    {
        using var host = new ResolverBoundaryHost();
        using var client = host.CreateClient();
        var routes = host.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains("GET") == true)
            .Select(endpoint => (Endpoint: endpoint, Contract: ListContract(endpoint.Metadata.GetMetadata<MethodInfo>())))
            .Where(entry => entry.Contract is not null).ToArray();
        foreach (var route in new[] { "/architecture/UserResponse/list", "/architecture/GroupResponse/list",
            "/architecture/RoleResponse/list", "/architecture/MembershipResponse/list",
            "/digital-processes/FormDefinitionResponse/list", "/digital-processes/RequestResponse/list" })
            Assert.Contains(routes, entry => entry.Endpoint.RoutePattern.RawText == route);
        foreach (var (endpoint, contract) in routes)
        {
            var arguments = contract!.GetGenericArguments();
            var spy = host.RegisteredSpy(arguments[2]);
            spy.Expect("VectorPageAsync", call =>
            {
                Assert.Equal(RbacOperations.Read, call.Arguments[1]);
                return typeof(GenericListBoundaryTests).GetMethod(nameof(EmptyPage), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(arguments[1]).Invoke(null, null);
            }, arguments[1]);
            using var request = ActingRequest.Get(endpoint.RoutePattern.RawText + "?take=5", Guid.NewGuid());
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("[]", await response.Content.ReadAsStringAsync());
            using var invalid = ActingRequest.Get(endpoint.RoutePattern.RawText + "?operation=create", Guid.NewGuid());
            using var rejected = await client.SendAsync(invalid);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
        host.Verify();
    }

    internal static MethodInfo? ListContract(MethodInfo? handler)
    {
        if (handler is null)
            return null;
        if (handler.DeclaringType == typeof(BaseResolvers) && handler.Name == nameof(BaseResolvers.List))
            return handler;
        return CompiledBoundaryInspector.ReferencedMembers(handler).OfType<MethodInfo>()
            .SingleOrDefault(method => method.DeclaringType == typeof(BaseResolvers) &&
                method.Name == nameof(BaseResolvers.List));
    }

    private static object EmptyPage<TDto>()
        => Task.FromResult(new VectorPageResult<TDto>([], new VectorPageInfo(5, 0, false)));
}
