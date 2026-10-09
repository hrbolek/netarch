using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Services;

public interface IMembershipService : IBaseService<MembershipModel, MembershipResponse>
{
    Task<MembershipResponse> CreateContainedAsync(MembershipCreateContainedRequest request, CancellationToken cancellationToken = default);
}
