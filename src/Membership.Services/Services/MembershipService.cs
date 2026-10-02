using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Membership.Models;

namespace WebAppArchitecture.Membership.Services;

public interface IMembershipService : IBaseService<MembershipModel, MembershipResponse>
{
}

public sealed class MembershipService : BaseService<MembershipModel, MembershipResponse, IMembershipStore>, IMembershipService
{
    public MembershipService(IMembershipStore store) : base(store)
    {
    }
}
