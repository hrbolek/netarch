using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Services;

/// <summary>
/// Služba I Membership Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IMembershipService : IBaseService<MembershipModel, MembershipResponse>
{
    Task<MembershipResponse> CreateContainedAsync(MembershipCreateContainedRequest request, CancellationToken cancellationToken = default);
}
