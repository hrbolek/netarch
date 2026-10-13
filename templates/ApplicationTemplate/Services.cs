using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
namespace RootNamespace.AppName;

public interface IFirstEntityService : IBaseService<FirstEntityModel> { }
public sealed class FirstEntityService(ISecurityLayer security, IDBLayer db)
    : SecuredBaseService<FirstEntityModel>(security, db), IFirstEntityService { }
public interface ISecondEntityService : IBaseService<SecondEntityModel> { }
public sealed class SecondEntityService(ISecurityLayer security, IDBLayer db)
    : SecuredBaseService<SecondEntityModel>(security, db), ISecondEntityService { }
