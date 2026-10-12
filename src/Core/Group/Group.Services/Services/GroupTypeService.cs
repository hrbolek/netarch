using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Services;

/// <summary>
/// Služba Group Type Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class GroupTypeService : BaseService<GroupTypeModel, GroupTypeResponse>, IGroupTypeService
{
    public GroupTypeService(ISecurityLayer security) : base(security) { }
}
