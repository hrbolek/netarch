using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Services;

public interface IMembershipService : IBaseService<MembershipModel, MembershipResponse>
{
}

public sealed class MembershipService : BaseService<MembershipModel, MembershipResponse>, IMembershipService
{
    public MembershipService(ISecurityLayer security) : base(security)
    {
    }
}
