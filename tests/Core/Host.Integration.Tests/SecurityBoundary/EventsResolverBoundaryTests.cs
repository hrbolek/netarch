using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Events;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class EventsResolverBoundaryTests
{
    [Theory]
    [InlineData("/events/EventResponse/list")]
    [InlineData("/events/EventTypeResponse/list")]
    [InlineData("/events/EventResponse/view/20000000-0000-0000-0000-000000000001")]
    [InlineData("/events/EventResponse/children/20000000-0000-0000-0000-000000000001")]
    public async Task AnonymousRequestsStopBeforeConcreteServiceTouchesItsDependencies(string path)
    {
        var (db, dbSpy) = StrictServiceSpy.Create<IDBLayer>();
        var (authorization, authorizationSpy) = StrictServiceSpy.Create<IAuthorizationLayer>();
        using var catalog = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>().Options);
        var types = new EventTypeService(authorization, db);
        var events = new EventService(authorization, db, catalog, types);
        using var host = new ResolverBoundaryHost();
        using var factory = host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            // Sealed services remain real. Their dependency spies reject every
            // operation; the catalog has no database provider or connection.
            services.RemoveAll<EventTypeService>();
            services.RemoveAll<EventService>();
            services.AddSingleton(types);
            services.AddSingleton(events);
        }));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path + "?actorUserId=" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        dbSpy.Verify();
        authorizationSpy.Verify();
        host.Verify();
    }
}
