using System.Linq.Expressions;
using System.Reflection;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Queries;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Base.Security;
using Xunit;

namespace Core.Contracts.Tests;

public sealed class ContractTests
{
    [Fact]
    public void ContractsHaveNoImplementationOrFrameworkDependencies()
    {
        var assembly = typeof(IEntityDBModel).Assembly;
        Assert.Equal("Core.Contracts", assembly.GetName().Name);
        Assert.All(assembly.GetReferencedAssemblies(), reference =>
            Assert.StartsWith("System.", reference.Name));
        Assert.All(new[]
        {
            typeof(ISecuredEntityDBModel), typeof(IEntityDTOModel), typeof(ISecuredEntityDTOModel),
            typeof(ICreateDTOModel<>), typeof(IUpdateDTOModel<>), typeof(IEntityProjection<,>),
            typeof(IEntityMapper<,>), typeof(IEntityWithTree),
            typeof(ISecuredEntityService<>), typeof(IRbacAuthorizationService),
            typeof(RbacOperations), typeof(RbacOperationIds), typeof(RbacWellKnown)
        }, type => Assert.Same(assembly, type.Assembly));
        Assert.DoesNotContain(assembly.GetTypes(), type =>
            type.IsClass && type.Name is "AuthorizedQueries" or "AuthorizedQuery`1");
    }

    [Fact]
    public void ExistingStaticAndDefaultContractsRemainUsable()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);
        var created = Create(new CreateRequest(id));
        Assert.Equal(entity, created);
        Assert.Equal(entity, Apply<TestEntity, UpdateRequest>(new UpdateRequest(id), entity));
        Assert.Equal(id, Map<TestEntity, TestDto>(entity).id);
        Assert.Equal(id, Project<TestEntity, TestDto>(entity).id);
        Assert.Equal(id.ToString(), ((IEntityDTOModel)new TestDto(id)).visual_label);
        Assert.Empty(typeof(ISecuredEntityService<>).GetInterfaces());
        Assert.Null(typeof(ISecuredEntityService<>).Assembly.GetType("WebAppArchitecture.Base.Services.IBaseService`1"));
        Assert.Equal(new VectorPageInfo(10, 1, false), new VectorPageResult<TestDto>([], new(10, 1, false)).PageInfo);
        Assert.True(new VectorPageInfo(10, 1, false).HasPrevious);
        Assert.Equal(new VectorQueryOptions(), VectorQueryOptions.Empty);
    }

    [Fact]
    public void AuthorizedQueryExposesOnlyTheSpecifiedSafeSurface()
    {
        var query = typeof(IAuthorizedQuery<TestEntity>);
        Assert.Empty(query.GetInterfaces());
        Assert.Equal(new[]
        {
            "AnyAsync", "CountAsync", "FirstOrDefaultAsync", "OrderBy",
            "OrderByDescending", "Skip", "Take", "ToListAsync", "Where"
        }, query.GetMethods().Select(method => method.Name).Order().ToArray());
        Assert.Equal(typeof(Expression<Func<TestEntity, bool>>),
            query.GetMethod("Where")!.GetParameters().Single().ParameterType);
        foreach (var name in new[] { "OrderBy", "OrderByDescending" })
        {
            var method = query.GetMethod(name)!;
            Assert.True(method.IsGenericMethodDefinition);
            Assert.Equal(typeof(Expression<>), method.GetParameters().Single().ParameterType.GetGenericTypeDefinition());
            Assert.Equal(query, method.ReturnType);
        }
        foreach (var name in new[] { "ToListAsync", "FirstOrDefaultAsync", "CountAsync", "AnyAsync" })
        {
            var parameter = query.GetMethod(name)!.GetParameters().Single();
            Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
            Assert.True(parameter.IsOptional);
        }
        Assert.Equal(typeof(Task<List<TestEntity>>), query.GetMethod("ToListAsync")!.ReturnType);
        var nullable = new NullabilityInfoContext().Create(query.GetMethod("FirstOrDefaultAsync")!.ReturnParameter);
        Assert.Equal(NullabilityState.Nullable, nullable.GenericTypeArguments.Single().ReadState);
    }

    [Fact]
    public void AuthorizedSourcesRequireSecuredModelsWithoutAnActorOverride()
    {
        var read = Assert.Single(typeof(IAuthorizedQueries).GetMethods());
        Assert.Equal("Read", read.Name);
        Assert.Empty(read.GetParameters());
        Assert.Equal(typeof(IAuthorizedQuery<>), read.ReturnType.GetGenericTypeDefinition());
        foreach (var parameter in new[]
        {
            read.GetGenericArguments().Single(),
            typeof(IAuthorizedQuery<>).GetGenericArguments().Single()
        })
        {
            Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
            Assert.Equal(typeof(ISecuredEntityDBModel), Assert.Single(parameter.GetGenericParameterConstraints()));
        }
    }

    private static TestEntity Create(ICreateDTOModel<TestEntity> request) => request.ToModel();
    private static T Apply<T, TRequest>(TRequest request, T entity)
        where T : IEntityDBModel
        where TRequest : IUpdateDTOModel<T> => request.ApplyTo(entity);
    private static TDto Map<T, TDto>(T entity)
        where T : IEntityDBModel
        where TDto : IEntityMapper<T, TDto> => TDto.FromModel(entity);
    private static TDto Project<T, TDto>(T entity)
        where T : IEntityDBModel
        where TDto : IEntityProjection<T, TDto> => TDto.Projection.Compile()(entity);

    private sealed record TestEntity(Guid id) : ISecuredEntityDBModel
    {
        public Guid rbacobject_id => id;
        public DateTimeOffset created_at => default;
        public DateTimeOffset updated_at => default;
        public Guid created_by => default;
        public Guid updated_by => default;
    }

    private sealed record TestDto(Guid id) : IEntityProjection<TestEntity, TestDto>, IEntityMapper<TestEntity, TestDto>
    {
        public static Expression<Func<TestEntity, TestDto>> Projection => entity => new TestDto(entity.id);
        public static TestDto FromModel(TestEntity model) => new(model.id);
    }

    private sealed record CreateRequest(Guid Id) : ICreateDTOModel<TestEntity>
    {
        public TestEntity ToModel() => new(Id);
    }

    private sealed record UpdateRequest(Guid id) : IUpdateDTOModel<TestEntity>
    {
        public DateTimeOffset updated_at => default;
        public TestEntity ApplyTo(TestEntity model) => model with { id = id };
    }
}
