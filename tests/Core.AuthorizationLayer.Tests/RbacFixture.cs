using WebAppArchitecture.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace Core.AuthorizationLayer.Tests;

/// <summary>Fresh, isolated, real relational database per test; never use EF InMemory provider.</summary>
public sealed class RbacFixture : IDisposable
{
    private readonly PostgresTestDatabase database = new();
    private readonly ServiceProvider services;
    public CoreDbContext Context { get; }
    public IDBLayer Db { get; }
    public SecurityProjectionService Projection { get; }
    public Guid Student { get; } = Guid.NewGuid();
    public Guid Dean { get; } = Guid.NewGuid();
    public Guid ApplicantType { get; } = Guid.NewGuid();
    public Guid DeanType { get; } = Guid.NewGuid();
    public Guid ReaderType { get; } = Guid.NewGuid();
    public Guid GroupType { get; } = Guid.NewGuid();
    public Guid Root { get; } = Guid.NewGuid();
    public Guid Faculty { get; } = Guid.NewGuid();
    public Guid OtherFaculty { get; } = Guid.NewGuid();
    public Guid Request { get; } = Guid.NewGuid();
    public Guid Visible { get; } = Guid.NewGuid();
    public Guid Secret { get; } = Guid.NewGuid();
    public Guid SecretChild { get; } = Guid.NewGuid();
    public Guid Sibling { get; } = Guid.NewGuid();
    public DateTimeOffset Now { get; } = DateTimeOffset.UtcNow;
    public CoreDbContext CreateContext() => new(new DbContextOptionsBuilder<CoreDbContext>()
        .UseNpgsql(database.ConnectionString).Options);
    public RbacFixture(Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor? interceptor = null)
    {
        var servicesCollection = new ServiceCollection();
        services = servicesCollection.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<CoreDbContext>().UseNpgsql(database.ConnectionString);
        if (interceptor is not null) options.AddInterceptors(interceptor);
        Context = new CoreDbContext(options.Options);
        Context.Database.EnsureCreated();
        Db = new DBLayer(Context, services);
        Projection = new SecurityProjectionService(Db);
        Context.GroupTypes.Add(new GroupTypeModel(GroupType,"Organizational",null,$"/{GroupType:D}/",Root,Now,Now,Student,Student));
        AddGroup(Root,null,"University"); AddGroup(Faculty,Root,"Faculty"); AddGroup(OtherFaculty,Root,"OtherFaculty");
        AddGroup(Request,Faculty,"Request"); AddGroup(Visible,Request,"Visible"); AddGroup(Secret,Request,"Secret"); AddGroup(SecretChild,Secret,"SecretChild"); AddGroup(Sibling,Request,"Sibling");
        foreach (var (id,name) in new[]{(ApplicantType,"Applicant"),(DeanType,"Dean"),(ReaderType,"Reader")})
            Context.RoleTypes.Add(new RoleTypeModel(id,name,Root,Now,Now,Student,Student));
        Context.Users.Add(new UserModel(Student,"Student",null,null,Root,Now,Now,Student,Student));
        Context.Users.Add(new UserModel(Dean,"Dean",null,null,Root,Now,Now,Dean,Dean));
        Context.SaveChanges();
    }
    public GroupModel AddGroup(Guid id, Guid? parent, string name, bool inherit=true)
    {
        string path = parent is null ? $"/{id:D}/" : Context.Groups.Local.First(x=>x.id==parent).path+$"{id:D}/";
        // Each group is secured by its own RBAC object. The former Root value
        // made all GroupModel rows require permission on Root instead of their own group.
        var g = new GroupModel(id,name,GroupType,parent,path,inherit,id,Now,Now,Student,Student);
        Context.Groups.Add(g); return g;
    }
    public Guid AddRole(Guid user,Guid group,Guid roleType, DateTimeOffset? start=null,DateTimeOffset? end=null)
    {
        var id=Guid.NewGuid(); Context.Roles.Add(new RoleModel(id,group,user,roleType,false,start??Now.AddDays(-1),end,null,group,Now,Now,Student,Student)); Context.SaveChanges(); return id;
    }
    public Guid Deny(Guid group,Guid roleType,bool descendants=true)
    {
        var id=Guid.NewGuid(); Context.RbacDenyRules.Add(new RbacDenyRuleModel(id,group,roleType,descendants,group,Now,Now,Student,Student)); Context.SaveChanges(); return id;
    }
    public Guid Bind(Guid source,Guid sourceType,Guid target,Guid targetType)
    {
        var id=Guid.NewGuid(); Context.RbacAccessBindings.Add(new RbacAccessBindingModel(id,source,sourceType,target,targetType,Now.AddDays(-1),null,null,source,Now,Now,Student,Student)); Context.SaveChanges(); return id;
    }
    public void Operation(Guid roleType,string name)
    {
        var op=Context.Operations.Local.FirstOrDefault(x=>x.name==name) ?? Context.Operations.FirstOrDefault(x=>x.name==name);
        if(op is null){op=new OperationModel(RbacOperationIds.FromName(name),name,Now,Now,Student,Student);Context.Operations.Add(op);Context.SaveChanges();}
        Context.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(),roleType,op.id));Context.SaveChanges();
    }
    public bool Has(Guid user,Guid group,Guid roleType)
    {
        long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Context.EffectiveAccesses.AsNoTracking().Any(x=>x.user_id==user && x.rbacobject_id==group && x.role_type_id==roleType && x.valid_from_unix_ms<=now && (x.valid_to_unix_ms==null || x.valid_to_unix_ms>=now));
    }
    public async Task Sync() => await Projection.RebuildAllAsync();
    public void Dispose(){Context.Dispose();services.Dispose();database.Dispose();}
}
