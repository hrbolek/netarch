using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.Shared;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class ServiceCompositionTests
{
    public static IEnumerable<object[]> AuxiliaryServices() =>
        new[]
        {
            typeof(FormSearchService), typeof(FormNodeInsertionService), typeof(FormComponentCatalogService),
            typeof(FormDesignAiService), typeof(FormDocumentService), typeof(ApplicantPolicyService),
            typeof(RtfTemplateService), typeof(DigitalProcessActionService<FormVersionModel>)
        }.Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(AuxiliaryServices))]
    public void AuxiliaryServicesOnlyComposeEntityServicesAndNeverOwnPersistence(Type type)
    {
        Assert.Null(SecuredModel(type));
        var dependencies = type.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        Assert.DoesNotContain(dependencies, dependency => dependency == typeof(ISecurityLayer) ||
            typeof(IDBLayer).IsAssignableFrom(dependency) ||
            typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(dependency) ||
            dependency == typeof(RtfTemplateStore));
        Assert.Contains(dependencies, dependency => dependency.GetInterfaces().Append(dependency)
            .Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IBaseService<>)));
    }

    [Fact]
    public void EachSecuredEntityHasExactlyOneCorrectlyNamedService()
    {
        var entities = typeof(FormVersionModel).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ISecuredEntityDBModel).IsAssignableFrom(type)).ToArray();
        var services = typeof(FormVersionService).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && SecuredModel(type) is not null).ToArray();
        Assert.Equal(16, entities.Length);
        Assert.Equal(entities.Length, services.Length);
        foreach (var entity in entities)
        {
            var service = Assert.Single(services, type => SecuredModel(type) == entity);
            Assert.Equal(entity.Name[..^"Model".Length] + "Service", service.Name);
        }
    }

    [Fact]
    public void EntityAliasesShareScopedInstancesAndAllComposedServicesResolve()
    {
        using var host = new IsolatedHost();
        using var scope = host.Services.CreateScope();
        using var other = host.Services.CreateScope();
        var models = typeof(FormVersionModel).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ISecuredEntityDBModel).IsAssignableFrom(type));
        foreach (var model in models)
        {
            var contract = typeof(IBaseService<>).MakeGenericType(model);
            var instance = scope.ServiceProvider.GetRequiredService(contract);
            Assert.Equal(model, SecuredModel(instance.GetType()));
            var named = instance.GetType().GetInterfaces().Single(type => type.Name == "I" + instance.GetType().Name);
            Assert.Same(instance, scope.ServiceProvider.GetRequiredService(named));
            Assert.NotSame(instance, other.ServiceProvider.GetRequiredService(contract));
            var actions = typeof(IDigitalProcessActionService<>).MakeGenericType(model);
            Assert.Null(SecuredModel(scope.ServiceProvider.GetRequiredService(actions).GetType()));
        }
        foreach (var data in AuxiliaryServices())
        {
            var implementation = (Type)data[0];
            if (implementation.IsGenericType) continue;
            var contract = implementation.GetInterfaces().Single(type => type.Name == "I" + implementation.Name);
            Assert.Equal(implementation, scope.ServiceProvider.GetRequiredService(contract).GetType());
        }
    }

    private static Type? SecuredModel(Type? type)
    {
        for (; type is not null; type = type.BaseType)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SecuredBaseService<>))
                return type.GenericTypeArguments[0];
        return null;
    }
}
