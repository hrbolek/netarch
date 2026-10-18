using WebAppArchitecture.Base.Services;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Services;

/// <summary>
/// Služba I Group Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IGroupService : ISecuredEntityService<GroupModel>
{
    Task<GroupResponse> CreateContainedAsync(GroupCreateContainedRequest request, CancellationToken cancellationToken = default);
    Task<GroupResponse> CreateTypedAsync(GroupCreateTypedRequest request, CancellationToken cancellationToken = default);
}
