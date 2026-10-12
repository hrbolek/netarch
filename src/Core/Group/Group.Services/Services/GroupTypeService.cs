using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Services;

/// <summary>
/// Služba Group Type Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class GroupTypeService : SecuredBaseService<GroupTypeModel>, IGroupTypeService
{
    public GroupTypeService(ISecurityLayer security, IDBLayer unsecuredDb) : base(security, unsecuredDb) { }
}
