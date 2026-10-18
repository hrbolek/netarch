using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.GroupType.Services;

/// <summary>
/// Služba I Group Type Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IGroupTypeService : ISecuredEntityService<GroupTypeModel> { }
