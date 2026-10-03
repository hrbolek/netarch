using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

public interface IUserStore : IBaseStore<UserModel> { }
public interface IGroupStore : IBaseStore<GroupModel> { }
public interface IGroupTypeStore : IBaseStore<GroupTypeModel> { }
public interface IMembershipStore : IBaseStore<MembershipModel> { }
public interface IRoleStore : IBaseStore<RoleModel> { }
public interface IRoleTypeStore : IBaseStore<RoleTypeModel> { }
