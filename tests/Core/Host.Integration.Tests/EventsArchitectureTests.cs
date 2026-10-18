using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Events;
using WebAppArchitecture.AuthorizationLayer;
using Xunit;

namespace Core.Host.Integration.Tests;

public sealed class EventsArchitectureTests
{
    [Fact]
    public void ResolversDoNotDependOnDatabaseOrRawSecurity()
    {
        var parameters = typeof(EventsResolvers)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(method => method.GetParameters());
        Assert.All(parameters, parameter =>
        {
            Assert.False(typeof(DbContext).IsAssignableFrom(parameter.ParameterType));
            Assert.False(typeof(IDBLayer).IsAssignableFrom(parameter.ParameterType));
            Assert.False(typeof(IAuthorizationLayer).IsAssignableFrom(parameter.ParameterType));
            Assert.False(typeof(IRbacAuthorizationService).IsAssignableFrom(parameter.ParameterType));
        });
    }

    [Fact]
    public void AdministrationIsScopedSecuredAndIndependentOfUi()
    {
        Assert.True(typeof(SecuredEntityService<EventModel>).IsAssignableFrom(typeof(EventAdministrationService)));
        Assert.DoesNotContain(typeof(EventAdministrationService).Assembly.GetReferencedAssemblies(),
            assembly => assembly.Name?.EndsWith(".UI", StringComparison.Ordinal) == true);
        var services = new ServiceCollection();
        new EventsModule().ConfigureServices(services, new ConfigurationBuilder().Build());
        var registration = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(EventAdministrationService));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
        Assert.Equal(typeof(EventAdministrationService), registration.ImplementationType);
    }

    [Fact]
    public async Task FormsAndImportsRejectMissingActorBeforeCallingServices()
    {
        var http = new DefaultHttpContext();
        var actors = new ActorProvider(null);
        var ct = CancellationToken.None;
        var results = new object[]
        {
            await EventsResolvers.EventEditPage(Guid.NewGuid(), http, actors, null!, null!, ct),
            await EventsResolvers.EventTypeEditPage(Guid.NewGuid(), http, actors, null!, null!, ct),
            await EventsResolvers.EventForm(http, actors, null!, ct),
            await EventsResolvers.EventImportJsonPost(http.Request, http, actors, null!, ct),
            await EventsResolvers.EventTypeImportStandardPost(new StandardTypeImportRequest(Guid.NewGuid()), http, actors, null!, ct)
        };
        Assert.All(results, result =>
            Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode));
    }

    [Fact]
    public async Task JsonImportValidatesTransportAndScopeBeforeCallingService()
    {
        var http = new DefaultHttpContext();
        var actors = new ActorProvider(Guid.NewGuid());
        var invalidTransport = await EventsResolvers.EventImportJsonPost(http.Request, http, actors, null!, CancellationToken.None);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(invalidTransport).StatusCode);

        http.Request.ContentType = "multipart/form-data; boundary=events";
        http.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        var missingScope = await EventsResolvers.EventImportJsonPost(http.Request, http, actors, null!, CancellationToken.None);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(missingScope).StatusCode);
    }

    private sealed record ActorProvider(Guid? Actor) : ICurrentActorProvider
    {
        public Guid? TryGetActorUserId(HttpContext httpContext) => Actor;
    }
}
