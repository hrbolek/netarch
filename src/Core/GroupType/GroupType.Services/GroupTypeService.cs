using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.GroupType.Services;

/// <summary>
/// Služba Group Type Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public sealed class GroupTypeService : SecuredEntityService<GroupTypeModel>, IGroupTypeService
{
    public GroupTypeService(IAuthorizationLayer security, IDBLayer unsecuredDb) : base(security, unsecuredDb) { }
}
