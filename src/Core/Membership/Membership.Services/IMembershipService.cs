using WebAppArchitecture.Base.Services;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Services;

/// <summary>
/// Služba I Membership Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IMembershipService : ISecuredEntityService<MembershipModel>
{
    Task<MembershipResponse> CreateContainedAsync(MembershipCreateContainedRequest request, CancellationToken cancellationToken = default);
}
