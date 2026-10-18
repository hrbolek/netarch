using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;
namespace RootNamespace.AppName;

public interface IFirstEntityService : ISecuredEntityService<FirstEntityModel> { }
public sealed class FirstEntityService(IAuthorizationLayer security, IDBLayer db)
    : SecuredEntityService<FirstEntityModel>(security, db), IFirstEntityService { }
public interface ISecondEntityService : ISecuredEntityService<SecondEntityModel> { }
public sealed class SecondEntityService(IAuthorizationLayer security, IDBLayer db)
    : SecuredEntityService<SecondEntityModel>(security, db), ISecondEntityService { }
